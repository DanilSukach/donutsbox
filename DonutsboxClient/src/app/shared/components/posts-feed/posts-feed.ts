import { Component, effect, inject, input, signal, ElementRef, ViewChild, AfterViewInit, OnDestroy, untracked } from '@angular/core';
import { PostCard } from '@app/shared/components/post-card/post-card';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { Observable } from 'rxjs';

export type FeedMode = 'creator' | 'subscriptions';

@Component({
  selector: 'app-posts-feed',
  standalone: true,
  imports: [PostCard],
  templateUrl: './posts-feed.html',
  styleUrl: './posts-feed.css'
})
export class PostsFeed implements AfterViewInit, OnDestroy {
  // Inputs
  readonly mode = input.required<FeedMode>(); // 'creator' или 'subscriptions'
  readonly creatorId = input<string>(); // Только для режима 'creator'
  readonly loadPostsFunction = input.required<(page: number, pageSize: number) => Observable<any>>(); // Функция загрузки
  readonly showAuthorInfo = input<boolean>(false); // Показывать ли аватарку автора
  
  private postsRefreshService = inject(PostsRefresh);
  private tokenService = inject(TokenService);
  private jwtService = inject(JwtDecodeService);

  // State
  readonly posts = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly hasMore = signal(true);
  readonly currentPage = signal(1);
  readonly pageSize = 10;
  readonly error = signal<string | null>(null);

  @ViewChild('sentinel', { read: ElementRef }) sentinel?: ElementRef;
  private observer?: IntersectionObserver;
  private lastTrigger = -1;

  constructor() {
    effect(() => {
      const trigger = this.postsRefreshService.refreshTrigger();
      
      // Избегаем повторной загрузки для того же trigger
      if (trigger === this.lastTrigger) {
        return;
      }
      
      this.lastTrigger = trigger;
      console.log('🔄 posts-feed: effect сработал, trigger:', trigger);
      
      // Используем untracked для избежания бесконечного цикла
      untracked(() => {
        this.resetAndLoad();
      });
    });
  }

  ngAfterViewInit(): void {
    this.setupInfiniteScroll();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private setupInfiniteScroll(): void {
    if (!this.sentinel) return;

    this.observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry.isIntersecting && !this.isLoading() && this.hasMore()) {
          console.log('📍 Sentinel видим, загружаем еще...');
          this.loadMore();
        }
      },
      { threshold: 0.1 }
    );

    this.observer.observe(this.sentinel.nativeElement);
  }

  isPostOwner(post: any): boolean {
    const token = this.tokenService.getAccessToken();
    const currentUserGuid = this.jwtService.getGuid(token);
    
    // В режиме creator проверяем по creatorId
    if (this.mode() === 'creator') {
      return currentUserGuid === this.creatorId();
    }
    
    // В режиме subscriptions проверяем по post.creatorId
    return currentUserGuid === post.creatorId;
  }

  resetAndLoad(): void {
    console.log('🔄 Сброс и загрузка постов');
    this.posts.set([]);
    this.currentPage.set(1);
    this.hasMore.set(true);
    this.error.set(null);
    this.loadPosts();
  }

  private loadPosts(): void {
    if (this.isLoading()) return;

    this.isLoading.set(true);
    this.error.set(null);

    const loadFunction = this.loadPostsFunction();
    
    loadFunction(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        console.log('✅ Посты загружены:', response);
        
        const newPosts = response.posts || [];
        
        // Для первой страницы заменяем, для остальных добавляем
        if (this.currentPage() === 1) {
          this.posts.set(newPosts);
        } else {
          this.posts.update(existing => [...existing, ...newPosts]);
        }

        // Проверяем есть ли еще посты
        const total = response.total || 0;
        const loadedCount = this.currentPage() * this.pageSize;
        this.hasMore.set(loadedCount < total);

        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('❌ Ошибка загрузки постов:', err);
        this.error.set('Не удалось загрузить посты');
        this.isLoading.set(false);
      }
    });
  }

  loadMore(): void {
    if (!this.hasMore() || this.isLoading()) return;
    
    this.currentPage.update(p => p + 1);
    this.loadPosts();
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('ru-RU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}

