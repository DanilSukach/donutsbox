import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  AuthorsService,
  CreatorPageDataDto,
  FilesService,
  SubscriptionCreateDto,
  SubscriptionDto,
} from '@app/api/donutsbox';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class ProfileFacade {
  private readonly authorsService = inject(AuthorsService);
  private readonly filesService = inject(FilesService);
  private readonly tokenService = inject(TokenService);
  private readonly jwtService = inject(JwtDecodeService);
  private readonly router = inject(Router);

  readonly isCreatingProfile = signal(false);
  readonly isCreatingSubscription = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly subscriptionError = signal<string | null>(null);

  readonly isUploadingAvatar = signal(false);
  readonly isUploadingBanner = signal(false);
  readonly imageUploadError = signal<string | null>(null);

  uploadAvatar(file: File): Observable<string | null> {
    this.isUploadingAvatar.set(true);
    this.imageUploadError.set(null);

    return this.filesService.apiFilesImagesAvatarPost(file).pipe(
      map((resp) => resp.key ?? null),
      tap(() => this.isUploadingAvatar.set(false)),
      catchError((err: HttpErrorResponse) => {
        this.isUploadingAvatar.set(false);
        this.imageUploadError.set(this.normalizeUploadError(err));
        return of(null);
      })
    );
  }

  uploadBanner(file: File): Observable<string | null> {
    this.isUploadingBanner.set(true);
    this.imageUploadError.set(null);

    return this.filesService.apiFilesImagesBannerPost(file).pipe(
      map((resp) => resp.key ?? null),
      tap(() => this.isUploadingBanner.set(false)),
      catchError((err: HttpErrorResponse) => {
        this.isUploadingBanner.set(false);
        this.imageUploadError.set(this.normalizeUploadError(err));
        return of(null);
      })
    );
  }

  private normalizeUploadError(err: HttpErrorResponse): string {
    if (err.status === 400) return err.error?.message ?? 'Неверный файл';
    if (err.status === 401) return 'Не авторизован';
    if (err.status === 413) return 'Файл слишком большой';
    if (err.status >= 500) return 'Ошибка сервера при загрузке';
    return 'Не удалось загрузить файл. Попробуйте снова';
  }

  getImageUrl(key: string, ttl = 300): Observable<string | null> {
    if (!key) return of(null);
    return this.filesService.apiFilesImagesUrlGet(key, ttl).pipe(
      map((r) => r.url ?? null),
      catchError(() => of(null))
    );
  }

  getAuthorById(id: string) {
    return this.authorsService.apiAuthorsIdGet(id).pipe(catchError(() => of(null)));
  }
  
  createCreatorPage(creatorData: CreatorPageDataDto): Observable<any> {
    this.isCreatingProfile.set(true);
    this.profileError.set(null);

    return this.authorsService.apiAuthorsCreatorPost(creatorData).pipe(
      tap(() => {
        this.tokenService.clearNewCreatorStatus();
        this.isCreatingProfile.set(false);
        this.router.navigate(['/profile/subscription-setup']);
      }),
      catchError((error) => {
        this.isCreatingProfile.set(false);
        this.profileError.set(
          error.error?.message || 'Произошла ошибка при создании страницы. Попробуйте снова.'
        );
        return of(null);
      })
    );
  }

  createSubscription(
    subscriptionData: SubscriptionCreateDto,
    options?: { navigateOnSuccess?: boolean }
  ): Observable<SubscriptionDto | null> {
    const shouldNavigate = options?.navigateOnSuccess ?? true;
    this.isCreatingSubscription.set(true);
    this.subscriptionError.set(null);

    return this.authorsService.apiAuthorsSubscriptionPost(subscriptionData).pipe(
      tap(() => {
        this.isCreatingSubscription.set(false);
        if (shouldNavigate) {
          this.navigateToProfile();
        }
      }),
      catchError((error) => {
        this.isCreatingSubscription.set(false);
        this.subscriptionError.set(
          error.error?.message || 'Произошла ошибка при создании подписки. Попробуйте снова.'
        );
        return of(null);
      })
    );
  }

  skipSubscription(): void {
    this.navigateToProfile();
  }

  private navigateToProfile(): void {
    const token = this.tokenService.getAccessToken();
    const userId = this.jwtService.getGuid(token);

    if (userId) {
      this.router.navigate(['/profile', userId]);
    } else {
      this.router.navigate(['/']);
    }
  }

  getCurrentUserGuid(): string | null {
    const token = this.tokenService.getAccessToken();
    return this.jwtService.getGuid(token);
  }

  clearErrors(): void {
    this.profileError.set(null);
    this.subscriptionError.set(null);
  }
}
