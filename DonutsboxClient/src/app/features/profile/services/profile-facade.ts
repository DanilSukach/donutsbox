import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  AuthorsService,
  AuthorNameDto,
  AuthorDescriptionDto,
  CreatorPageDataDto,
  FilesService,
  SubscriptionCreateDto,
  SubscriptionDto,
  UpdateImageKeyDto,
  UserDataService,
  UserService,
} from '@app/api/donutsbox';
import { SessionService } from '@app/core/services/session.service';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { HttpErrorResponse, HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class ProfileFacade {
  private readonly authorsService = inject(AuthorsService);
  private readonly userDataService = inject(UserDataService);
  private readonly userService = inject(UserService);
  private readonly filesService = inject(FilesService);
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  readonly isCreatingProfile = signal(false);
  readonly isCreatingSubscription = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly subscriptionError = signal<string | null>(null);

  readonly isUploadingAvatar = signal(false);
  readonly isUploadingBanner = signal(false);
  readonly imageUploadError = signal<string | null>(null);
  readonly isUploadingAudio = signal(false);
  readonly audioUploadError = signal<string | null>(null);

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

  uploadAudio(file: File, postId: string, title: string): Observable<{ audioId: string; status: string } | null> {
    this.isUploadingAudio.set(true);
    this.audioUploadError.set(null);

    // File уже является Blob, можно передать напрямую
    return this.filesService.apiFilesAudioPost(postId, title, file as Blob).pipe(
      map((resp) => ({ audioId: resp.audioId ?? '', status: resp.status ?? 'UPLOADING' })),
      tap(() => this.isUploadingAudio.set(false)),
      catchError((err: HttpErrorResponse) => {
        this.isUploadingAudio.set(false);
        this.audioUploadError.set(this.normalizeUploadError(err));
        return of(null);
      })
    );
  }

  getAudioUrl(key: string, ttl = 300): Observable<string | null> {
    if (!key) return of(null);
    return this.filesService.apiFilesAudioUrlGet(key, ttl).pipe(
      map((r) => r.url ?? null),
      catchError(() => of(null))
    );
  }

  getAuthorById(id: string) {
    return this.authorsService.apiAuthorsIdGet(id).pipe(
      catchError((error) => {
        // Пробрасываем ошибку 404 дальше для обработки в компоненте
        if (error?.status === 404) {
          return throwError(() => error);
        }
        // Для других ошибок возвращаем null
        return of(null);
      })
    );
  }
  
  createCreatorPage(creatorData: CreatorPageDataDto): Observable<any> {
    this.isCreatingProfile.set(true);
    this.profileError.set(null);

    return this.authorsService.apiAuthorsCreatorPost(creatorData).pipe(
      tap(() => {
        void this.sessionService.refreshSession().subscribe();
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
    this.sessionService.ensureSession().subscribe(() => {
      const userId = this.sessionService.userId();
      if (userId) {
        this.router.navigate(['/profile', userId]);
      } else {
        this.router.navigate(['/']);
      }
    });
  }

  getCurrentUserGuid(): string | null {
    return this.sessionService.userId();
  }

  clearErrors(): void {
    this.profileError.set(null);
    this.subscriptionError.set(null);
  }

  updateCreatorPageBanner(bannerKey: string): Observable<boolean> {
    const dto: UpdateImageKeyDto = { key: bannerKey };
    return this.authorsService.apiAuthorsBannerPut(dto).pipe(
      map(() => true),
      catchError((error) => {
        console.error('Error updating creator page banner:', error);
        return of(false);
      })
    );
  }

  updateUserAvatar(avatarKey: string): Observable<boolean> {
    const dto: UpdateImageKeyDto = { key: avatarKey };
    return this.userDataService.apiUserDataAvatarPut(dto).pipe(
      map(() => true),
      catchError((error) => {
        console.error('Error updating user avatar:', error);
        return of(false);
      })
    );
  }

  updateAuthorName(name: string): Observable<{ success: boolean; message?: string }> {
    const dto: AuthorNameDto = { name };
    return this.authorsService.apiAuthorsAuthorNamePut(dto).pipe(
      map(() => ({ success: true, message: 'Название страницы обновлено' })),
      catchError((error) => {
        let errorMessage = 'Не удалось обновить название';
        if (error.status === 400) {
          errorMessage = error.error?.message || 'Ошибка валидации';
        }
        return of({ success: false, message: errorMessage });
      })
    );
  }

  updateAuthorDescription(description: string): Observable<{ success: boolean; message?: string }> {
    const dto: AuthorDescriptionDto = { description };
    return this.authorsService.apiAuthorsAuthorDescriptionPut(dto).pipe(
      map(() => ({ success: true, message: 'Описание обновлено' })),
      catchError((error) => {
        let errorMessage = 'Не удалось обновить описание';
        if (error.status === 400) {
          errorMessage = error.error?.message || 'Ошибка валидации';
        }
        return of({ success: false, message: errorMessage });
      })
    );
  }

  updateUserName(name: string): Observable<{ success: boolean; message?: string }> {
    const dto = { name: name };
    return this.userService.apiUserUserNamePut(dto).pipe(
      map((response: any) => ({
        success: true,
        message: response?.message || 'Имя успешно обновлено'
      })),
      catchError((error) => {
        let errorMessage = 'Ошибка при обновлении имени';
        if (error.status === 400) {
          errorMessage = error.error?.message || 'Ошибка валидации';
        } else if (error.status === 0) {
          errorMessage = 'Нет соединения с сервером';
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        }
        return of({
          success: false,
          message: errorMessage
        });
      })
    );
  }
}
