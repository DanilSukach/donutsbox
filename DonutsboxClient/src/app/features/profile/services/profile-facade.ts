import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthorsService, CreatorPageDataDto, SubscriptionCreateDto, SubscriptionDto } from '@app/api/donutsbox';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { Observable, catchError, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProfileFacade {
  private readonly authorsService = inject(AuthorsService);
  private readonly tokenService = inject(TokenService);
  private readonly jwtService = inject(JwtDecodeService);
  private readonly router = inject(Router);

  // Signals для состояния
  readonly isCreatingProfile = signal(false);
  readonly isCreatingSubscription = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly subscriptionError = signal<string | null>(null);

  /**
   * Создание страницы создателя
   */
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
        this.profileError.set(error.error?.message || 'Произошла ошибка при создании страницы. Попробуйте снова.');
        return of(null);
      })
    );
  }

  /**
   * Создание подписки
   */
createSubscription(subscriptionData: SubscriptionCreateDto): Observable<SubscriptionDto | null> {
    this.isCreatingSubscription.set(true);
    this.subscriptionError.set(null);

    return this.authorsService.apiAuthorsSubscriptionPost(subscriptionData).pipe(
      tap(() => {
        this.isCreatingSubscription.set(false);
        this.navigateToProfile();
      }),
      catchError((error) => {
        this.isCreatingSubscription.set(false);
        this.subscriptionError.set(error.error?.message || 'Произошла ошибка при создании подписки. Попробуйте снова.');
        return of(null);
      })
    );
  }

  /**
   * Пропустить создание подписки
   */
  skipSubscription(): void {
    this.navigateToProfile();
  }

  /**
   * Навигация к профилю пользователя
   */
  private navigateToProfile(): void {
    const token = this.tokenService.getAccessToken();
    const userId = this.jwtService.getGuid(token);

    if (userId) {
      this.router.navigate(['/profile', userId]);
    } else {
      this.router.navigate(['/']);
    }
  }

  /**
   * Получение текущего GUID пользователя
   */
  getCurrentUserGuid(): string | null {
    const token = this.tokenService.getAccessToken();
    return this.jwtService.getGuid(token);
  }

  /**
   * Очистка состояния ошибок
   */
  clearErrors(): void {
    this.profileError.set(null);
    this.subscriptionError.set(null);
  }
}