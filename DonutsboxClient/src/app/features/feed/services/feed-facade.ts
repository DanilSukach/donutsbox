import { inject, Injectable, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AuthorsService, FilesService, CreatorPostService } from '@app/api/donutsbox';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { Observable, catchError, forkJoin, map, of, tap } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';
import { UserSubscriptionsFacade } from '@app/features/profile/services/user-subscriptions-facade';

@Injectable({
  providedIn: 'root'
})
export class FeedFacade {
 private readonly authorsService = inject(AuthorsService);
  private readonly filesService = inject(FilesService);
  private readonly creatorPostService = inject(CreatorPostService);
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);
  private readonly userSubscriptionsFacade = inject(UserSubscriptionsFacade);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // Состояние
  readonly topAuthors = signal<AuthorRequestDto[]>([]);
  readonly isLoadingTopAuthors = signal(false);
  readonly topAuthorsError = signal<string | null>(null);
  readonly topAuthorsLoaded = signal(false);

  // Состояние поиска
  readonly searchResults = signal<AuthorRequestDto[]>([]);
  readonly isLoadingSearch = signal(false);
  readonly searchError = signal<string | null>(null);

  readonly userGuid = signal<string | null>(null);
  readonly isLoadingUserData = signal(false);

  // Подписанные URL для аватаров по id автора
  readonly authorAvatarUrlMap = signal<Record<string, string>>({});

  // Computed для быстрой проверки подписок пользователя
  readonly userSubscribedAuthorIds = computed(() => {
    const subscriptions = this.userSubscriptionsFacade.subscriptions();
    return new Set(subscriptions.map(sub => sub.id).filter(id => id) as string[]);
  });

  constructor() {
    if (this.isBrowser) {
      this.initializeUserData();
      // Загружаем подписки пользователя при инициализации
      this.userSubscriptionsFacade.loadUserSubscriptions().subscribe();
    } else {
      this.isLoadingUserData.set(false);
    }
  }

  private initializeUserData(): void {
    if (!this.isBrowser) {
      this.isLoadingUserData.set(false);
      return;
    }
    this.isLoadingUserData.set(true);
    this.sessionService.ensureSession().subscribe(() => {
      this.userGuid.set(this.sessionService.userId());
      this.isLoadingUserData.set(false);
    });
  }

  loadTopAuthors(count: number = 10): Observable<AuthorRequestDto[]> {
    if (!this.isBrowser) {
      return of([]);
    }
    this.isLoadingTopAuthors.set(true);
    this.topAuthorsError.set(null);

    return this.authorsService.apiAuthorsTopGet(count).pipe(
      tap((authors) => {
        this.topAuthors.set(authors);
        this.isLoadingTopAuthors.set(false);
        this.topAuthorsLoaded.set(true);
        this.loadAvatarSignedUrls(authors);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки топ авторов:', error);
        this.topAuthorsError.set('Не удалось загрузить топ авторов');
        this.isLoadingTopAuthors.set(false);
        return of([]);
      })
    );
  }

  prefetchAuthorAvatars(authors: AuthorRequestDto[]): void {
    this.loadAvatarSignedUrls(authors);
  }

  searchAuthors(query: string): Observable<AuthorRequestDto[]> {
    if (!this.isBrowser) {
      return of([]);
    }
    if (!query.trim()) {
      this.searchResults.set([]);
      this.searchError.set(null);
      return of([]);
    }

    this.isLoadingSearch.set(true);
    this.searchError.set(null);

    return this.authorsService.apiAuthorsSearchGet(query).pipe(
      tap((authors) => {
        this.searchResults.set(authors);
        this.isLoadingSearch.set(false);
        this.loadAvatarSignedUrls(authors);
      }),
      catchError((error) => {
        console.error('Ошибка поиска авторов:', error);
        this.searchError.set('Не удалось выполнить поиск');
        this.isLoadingSearch.set(false);
        this.searchResults.set([]);
        return of([]);
      })
    );
  }

  clearSearch(): void {
    this.searchResults.set([]);
    this.searchError.set(null);
    this.isLoadingSearch.set(false);
  }

  private loadAvatarSignedUrls(authors: AuthorRequestDto[]): void {
    if (!this.isBrowser) {
      return;
    }
    const requests = authors
      .filter(a => !!a.id && !!a.avatarUrl)
      .map(a =>
        this.filesService.apiFilesImagesUrlGet(a.avatarUrl!, 300).pipe(
          map(r => ({ id: a.id!, url: r.url ?? '' }))
        )
      );

    if (requests.length === 0) {
      return;
    }

    forkJoin(requests).subscribe({
      next: pairs => {
        const currentMap = { ...this.authorAvatarUrlMap() };
        for (const p of pairs) {
          if (p.url) currentMap[p.id] = p.url;
        }
        this.authorAvatarUrlMap.set(currentMap);
      },
      error: () => {}
    });
  }

  subscribeToAuthor(authorId: string): Observable<boolean> {
    return of(true);
  }

  loadUserFeedContent(): Observable<any[]> {
    const guid = this.userGuid();
    if (!guid) return of([]);
    return of([]);
  }

  getSubscriptionFeed(page: number = 1, pageSize: number = 10): Observable<any> {
    if (!this.isBrowser) {
      return of({ posts: [], total: 0 });
    }
    return this.creatorPostService.apiCreatorPostFeedGet(page, pageSize).pipe(
      catchError((error) => {
        console.error('Error loading feed:', error);
        throw error;
      })
    );
  }

  refreshUserData(): void {
    if (!this.isBrowser) {
      return;
    }
    this.sessionService.refreshSession().subscribe(() => {
      this.userGuid.set(this.sessionService.userId());
    });
  }

  loadUserSubscriptions(): void {
    if (!this.isBrowser) {
      return;
    }
    this.userSubscriptionsFacade.loadUserSubscriptions().subscribe();
  }

  navigateToAuthor(authorId: string): void {
    if (!this.isBrowser) {
      return;
    }
    this.router.navigate(['/profile', authorId]);
  }

  clearUserData(): void {
    this.userGuid.set(null);
    this.topAuthors.set([]);
    this.isLoadingTopAuthors.set(false);
    this.topAuthorsError.set(null);
    this.topAuthorsLoaded.set(false);
    this.isLoadingUserData.set(false);
  }
}
