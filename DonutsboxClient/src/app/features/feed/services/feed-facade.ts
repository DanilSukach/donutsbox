import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthorsService } from '@app/api/donutsbox';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { Observable, catchError, of, tap } from 'rxjs';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';

@Injectable({
  providedIn: 'root'
})
export class FeedFacade {
  private readonly authorsService = inject(AuthorsService);
  private readonly tokenService = inject(TokenService);
  private readonly jwtService = inject(JwtDecodeService);
  private readonly router = inject(Router);

  // Signals для состояния
  readonly topAuthors = signal<AuthorRequestDto[]>([]);
  readonly isLoadingTopAuthors = signal(false);
  readonly topAuthorsError = signal<string | null>(null);

  readonly userGuid = signal<string | null>(null);
  readonly isLoadingUserData = signal(false);

  constructor() {
    this.initializeUserData();
  }

  private initializeUserData(): void {
    this.isLoadingUserData.set(true);
    
    const token = this.tokenService.getAccessToken();
    const guid = this.jwtService.getGuid(token);
    
    if (guid) {
      this.userGuid.set(guid);
      console.log('User GUID инициализирован:', guid);
    }
    
    this.isLoadingUserData.set(false);
  }

  loadTopAuthors(count: number = 10): Observable<AuthorRequestDto[]> {
    this.isLoadingTopAuthors.set(true);
    this.topAuthorsError.set(null);

    return this.authorsService.apiAuthorsTopGet(count).pipe(
      tap((authors) => {
        this.topAuthors.set(authors);
        this.isLoadingTopAuthors.set(false);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки топ авторов:', error);
        this.topAuthorsError.set('Не удалось загрузить топ авторов');
        this.isLoadingTopAuthors.set(false);
        return of([]);
      })
    );
  }

  subscribeToAuthor(authorId: string): Observable<boolean> {
    console.log('Подписка на автора:', authorId);
    return of(true);
  }

  loadUserFeedContent(): Observable<any[]> {
    const guid = this.userGuid();
    if (!guid) {
      console.warn('GUID пользователя не найден');
      return of([]);
    }

    console.log('Загрузка персонального контента для GUID:', guid);
    return of([]);
  }

  refreshUserData(): void {
    this.initializeUserData();
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
