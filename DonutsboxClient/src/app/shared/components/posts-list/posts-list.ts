import { Component, effect, inject, input, signal } from '@angular/core';
import { PostsFacade } from '../../../features/profile/services/posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { PostCard } from "@app/shared/components/post-card/post-card";
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';

@Component({
  selector: 'app-posts-list',
  imports: [PostCard],
  templateUrl: './posts-list.html',
  styleUrl: './posts-list.css'
})
export class PostsList {
  readonly creatorId = input.required<string>();
  
  private postsFacade = inject(PostsFacade);
  private postsRefreshService = inject(PostsRefresh);
  private tokenService = inject(TokenService); 
  private jwtService = inject(JwtDecodeService); 

  readonly posts = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly currentPage = signal(1);
  readonly totalPages = signal(1);
  readonly pageSize = 10;

  constructor() {
    effect(() => {
      const trigger = this.postsRefreshService.refreshTrigger();
      const creatorId = this.creatorId();
      
      console.log('🔄 posts-list: effect сработал');
      console.log('  - trigger:', trigger);
      console.log('  - creatorId:', creatorId);
      
      if (creatorId) {
        console.log('  ✅ Вызываю loadPosts()');
        this.loadPosts();
      }
    });
  }

  isPostOwner(): boolean {
    const token = this.tokenService.getAccessToken();
    const currentUserGuid = this.jwtService.getGuid(token);
    return currentUserGuid === this.creatorId();
  }

  loadPosts(): void {
    this.isLoading.set(true);
    
    this.postsFacade.getCreatorPosts(
      this.creatorId(),
      this.currentPage(),
      this.pageSize
    ).subscribe({
      next: (response) => {
        this.posts.set(response.posts || []);
        this.totalPages.set(Math.ceil((response.total || 0) / this.pageSize));
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
      }
    });
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadPosts();
    }
  }

  prevPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadPosts();
    }
  }
}