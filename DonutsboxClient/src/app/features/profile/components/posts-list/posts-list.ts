import { Component, inject, input, signal } from '@angular/core';
import { PostsFacade } from '../../services/posts-facade';
import { PostCard } from "../post-card/post-card";

@Component({
  selector: 'app-posts-list',
  imports: [PostCard],
  templateUrl: './posts-list.html',
  styleUrl: './posts-list.css'
})
export class PostsList {
readonly creatorId = input.required<string>();
  private postsFacade = inject(PostsFacade);

  readonly posts = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly currentPage = signal(1);
  readonly totalPages = signal(1);
  readonly pageSize = 10;

  ngOnInit(): void {
    this.loadPosts();
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
        this.totalPages.set(Math.ceil(response.total || 0 / this.pageSize));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Ошибка загрузки постов:', err);
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

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadPosts();
    }
  }
}
