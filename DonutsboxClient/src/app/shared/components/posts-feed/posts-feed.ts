import { Component, effect, inject, input, output, signal, ElementRef, ViewChild, AfterViewInit, OnDestroy, untracked, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { PostCard } from '@app/shared/components/post-card/post-card';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { SessionService } from '@app/core/services/session.service';
import { Observable } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';

export type FeedMode = 'creator' | 'subscriptions';

@Component({
  selector: 'app-posts-feed',
  standalone: true,
  imports: [PostCard, LucideAngularModule],
  templateUrl: './posts-feed.html',
  styleUrl: './posts-feed.css'
})
export class PostsFeed implements AfterViewInit, OnDestroy {
  // Inputs
  readonly mode = input.required<FeedMode>(); // 'creator' или 'subscriptions'
  readonly creatorId = input<string>(); // Только для режима 'creator'
  readonly loadPostsFunction = input.required<(page: number, pageSize: number) => Observable<any>>(); // Функция загрузки
  readonly showAuthorInfo = input<boolean>(false); // Показывать ли аватарку автора
  
  // Outputs
  readonly postHidden = output<any>(); // Событие когда пост скрыт (moved to drafts) - передаём весь объект поста
  
  private postsRefreshService = inject(PostsRefresh);
  private sessionService = inject(SessionService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);

  // State
  readonly posts = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly hasMore = signal(true);
  readonly currentPage = signal(1);
  readonly pageSize = 10;
  readonly error = signal<string | null>(null);
  readonly initialLoadCompleted = signal(false);
  readonly initialLoadError = signal(false);

  @ViewChild('sentinel', { read: ElementRef }) sentinel?: ElementRef;
  private observer?: IntersectionObserver;
  private lastTrigger = -1;

  constructor() {
    if (this.isBrowser) {
      this.sessionService.ensureSession().subscribe();
    }
    effect(() => {
      if (!this.isBrowser) {
        return;
      }
      const trigger = this.postsRefreshService.refreshTrigger();
      
      // Избегаем повторной загрузки для того же trigger
      if (trigger === this.lastTrigger) {
        return;
      }
      
      const isFirstLoad = this.lastTrigger === -1;
      this.lastTrigger = trigger;
      console.log('🔄 posts-feed: effect сработал, trigger:', trigger, 'isFirstLoad:', isFirstLoad);
      
      // Используем untracked для избежания бесконечного цикла
      untracked(() => {
        if (isFirstLoad || this.posts().length === 0) {
          // Первая загрузка - полный сброс
          this.hardReset();
        } else {
          // Обновление - мягкий refresh без мигания
          this.resetAndLoad();
        }
      });
    });
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) {
      return;
    }
    this.setupInfiniteScroll();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private setupInfiniteScroll(): void {
    if (!this.isBrowser || !this.sentinel) return;

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
    const currentUserGuid = this.sessionService.userId();
    
    // В режиме creator проверяем по creatorId
    if (this.mode() === 'creator') {
      return currentUserGuid === this.creatorId();
    }
    
    // В режиме subscriptions проверяем по post.creatorId
    return currentUserGuid === post.creatorId;
  }

  resetAndLoad(): void {
    if (!this.isBrowser) {
      return;
    }
    console.log('🔄 Мягкое обновление постов');
    this.softRefresh();
  }

  /**
   * Мягкое обновление - загружает первую страницу и добавляет новые посты
   * без сброса всего списка (без мигания)
   */
  private softRefresh(): void {
    if (this.isLoading()) return;

    this.isLoading.set(true);
    this.error.set(null);

    const loadFunction = this.loadPostsFunction();
    
    loadFunction(1, this.pageSize).subscribe({
      next: (response) => {
        console.log('✅ Soft refresh - посты загружены:', response);
        
        const newPosts = response.posts || [];
        const existingPosts = this.posts();
        
        // Находим новые посты, которых нет в текущем списке
        const existingIds = new Set(existingPosts.map((p: any) => p.postId || p.id));
        const trulyNewPosts = newPosts.filter((p: any) => !existingIds.has(p.postId || p.id));
        
        if (trulyNewPosts.length > 0) {
          console.log(`📥 Добавлено ${trulyNewPosts.length} новых постов`);
          // Добавляем новые посты в начало
          this.posts.update(existing => [...trulyNewPosts, ...existing]);
        } else {
          console.log('📭 Новых постов нет');
        }
        
        // Обновляем существующие посты (например, статус видео)
        this.posts.update(existing => {
          return existing.map((existingPost: any) => {
            const updatedPost = newPosts.find((p: any) => 
              (p.postId || p.id) === (existingPost.postId || existingPost.id)
            );
            return updatedPost || existingPost;
          });
        });

        if (!this.initialLoadCompleted()) {
          this.initialLoadCompleted.set(true);
        }
        this.initialLoadError.set(false);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('❌ Ошибка soft refresh:', err);
        this.error.set('Не удалось обновить посты');
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Полный сброс и загрузка (используется только при первой загрузке)
   */
  hardReset(): void {
    if (!this.isBrowser) {
      return;
    }
    console.log('🔄 Полный сброс и загрузка постов');
    this.posts.set([]);
    this.currentPage.set(1);
    this.hasMore.set(true);
    this.error.set(null);
    this.initialLoadCompleted.set(false);
    this.initialLoadError.set(false);
    this.loadPosts();
  }

  private loadPosts(): void {
    if (!this.isBrowser || this.isLoading()) return;

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

        if (!this.initialLoadCompleted()) {
          this.initialLoadCompleted.set(true);
        }
        this.initialLoadError.set(false);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('❌ Ошибка загрузки постов:', err);
        if (this.currentPage() === 1 && !this.initialLoadCompleted()) {
          this.initialLoadError.set(true);
        } else {
          this.error.set('Не удалось загрузить посты');
        }
        this.isLoading.set(false);
      }
    });
  }

  loadMore(): void {
    if (!this.isBrowser || !this.hasMore() || this.isLoading()) return;
    
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

  onPostDeleted(postId: string): void {
    this.posts.update(posts => posts.filter(p => p.id !== postId));
  }

  onPostHidden(postId: string): void {
    // Находим пост перед удалением, чтобы передать его в profile-page для добавления в черновики
    const post = this.posts().find(p => p.id === postId);
    this.posts.update(posts => posts.filter(p => p.id !== postId));
    if (post) {
      this.postHidden.emit(post); // Передаём весь объект поста
    }
  }

  navigateToCreatorProfile(post: any): void {
    if (post.creatorId) {
      this.router.navigate(['/profile', post.creatorId]);
    }
  }
}

