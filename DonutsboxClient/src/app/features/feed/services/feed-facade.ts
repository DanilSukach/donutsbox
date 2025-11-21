import { inject, Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { AuthorsService, FilesService, CreatorPostService } from '@app/api/donutsbox';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { Observable, catchError, forkJoin, map, of, tap } from 'rxjs';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { UserSubscriptionsFacade } from '@app/features/profile/services/user-subscriptions-facade';

@Injectable({
  providedIn: 'root'
})
export class FeedFacade {
 private readonly authorsService = inject(AuthorsService);
  private readonly filesService = inject(FilesService);
  private readonly creatorPostService = inject(CreatorPostService);
  private readonly tokenService = inject(TokenService);
  private readonly jwtService = inject(JwtDecodeService);
  private readonly router = inject(Router);
  private readonly userSubscriptionsFacade = inject(UserSubscriptionsFacade);

  // Состояние
  readonly topAuthors = signal<AuthorRequestDto[]>([]);
  readonly isLoadingTopAuthors = signal(false);
  readonly topAuthorsError = signal<string | null>(null);

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
    this.initializeUserData();
    // Загружаем подписки пользователя при инициализации
    this.userSubscriptionsFacade.loadUserSubscriptions().subscribe();
  }

  private initializeUserData(): void {
    this.isLoadingUserData.set(true);
    const token = this.tokenService.getAccessToken();
    const guid = this.jwtService.getGuid(token);
    if (guid) this.userGuid.set(guid);
    this.isLoadingUserData.set(false);
  }

  loadTopAuthors(count: number = 10): Observable<AuthorRequestDto[]> {
    this.isLoadingTopAuthors.set(true);
    this.topAuthorsError.set(null);

    return this.authorsService.apiAuthorsTopGet(count).pipe(
      tap((authors) => {
        this.topAuthors.set(authors);
        this.isLoadingTopAuthors.set(false);
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
    return this.creatorPostService.apiCreatorPostFeedGet(page, pageSize).pipe(
      tap((response) => {
        console.log('Feed posts loaded:', response);
      }),
      catchError((error) => {
        console.error('Error loading feed:', error);
        throw error;
      })
    );
  }

  refreshUserData(): void {
    this.initializeUserData();
  }

  loadUserSubscriptions(): void {
    this.userSubscriptionsFacade.loadUserSubscriptions().subscribe();
  }

  navigateToAuthor(authorId: string): void {
    this.router.navigate(['/profile', authorId]);
  }

  clearUserData(): void {
    this.userGuid.set(null);
    this.topAuthors.set([]);
    this.isLoadingTopAuthors.set(false);
    this.topAuthorsError.set(null);
    this.isLoadingUserData.set(false);
  }
}
