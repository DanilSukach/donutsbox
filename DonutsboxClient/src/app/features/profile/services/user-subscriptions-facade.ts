import { inject, Injectable, signal } from '@angular/core';
import { AuthorPreviewDto, UserDataService, UserInteractionService, UserSubscriptionService, UserSubscriptionDto } from '@app/api/donutsbox';
import { catchError, Observable, of, tap, switchMap } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';

@Injectable({
  providedIn: 'root'
})
export class UserSubscriptionsFacade {
  private readonly userDataService = inject(UserDataService);
  private readonly userInteraction = inject(UserInteractionService);
  private readonly userSubscriptionService = inject(UserSubscriptionService);
  private readonly sessionService = inject(SessionService);

  readonly subscriptions = signal<AuthorPreviewDto[]>([]);
  readonly userSubscriptions = signal<UserSubscriptionDto[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  loadUserSubscriptions(): Observable<AuthorPreviewDto[]> {
    this.isLoading.set(true);
    this.error.set(null);

    return this.userDataService.apiUserDataSubscriptionsGet().pipe(
      tap((subscriptions) => {
        this.subscriptions.set(subscriptions);
        this.isLoading.set(false);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки подписок:', error);
        this.error.set('Не удалось загрузить список подписок');
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

    unsubscribeFromCreator(creatorPageId: string): Observable<any> {
    this.error.set(null);

    return this.userInteraction.apiUserInteractionUnsubscribeUserCreatorUserIdDelete(creatorPageId).pipe(
      tap(() => {
        console.log('Отписка от создателя успешна:', creatorPageId);
        this.loadUserSubscriptions().subscribe();
      }),
      catchError((error) => {
        console.error('Ошибка отписки от создателя:', error);
        this.error.set('Не удалось отписаться от создателя');
        return of(null);
      })
    );
  }

  clearError(): void {
    this.error.set(null);
  }

  getCurrentSubscriptions(): AuthorPreviewDto[] {
    return this.subscriptions();
  }

  getIsLoading(): boolean {
    return this.isLoading();
  }

  getError(): string | null {
    return this.error();
  }

  /**
   * Загружает все подписки пользователя с их subscriptionId
   * Фильтрует только подписки текущего пользователя
   */
  loadUserSubscriptionsWithIds(): Observable<UserSubscriptionDto[]> {
    return this.sessionService.ensureSession().pipe(
      switchMap(() => {
        const currentUserId = this.sessionService.userId();
        if (!currentUserId) {
          console.warn('Пользователь не авторизован');
          this.userSubscriptions.set([]);
          return of([]);
        }

        return this.userSubscriptionService.apiUserSubscriptionGet().pipe(
          tap((subscriptions) => {
            console.log('Все подписки загружены:', subscriptions.length);
            console.log('Текущий userId:', currentUserId);
            
            // Фильтруем только подписки текущего пользователя
            const userSubscriptions = subscriptions.filter(sub => {
              // Проверяем, что userId подписки совпадает с текущим пользователем
              const matchesUser = sub.userId === currentUserId;
              return matchesUser;
            });
            
            console.log('Подписки текущего пользователя:', userSubscriptions.length);
            
            // Фильтруем только активные подписки
            const now = new Date();
            const activeSubscriptions = userSubscriptions.filter(sub => {
              const endDate = new Date(sub.endDate);
              return sub.status?.toLowerCase() === 'active' && endDate >= now;
            });
            
            console.log('Активные подписки текущего пользователя:', activeSubscriptions.length);
            this.userSubscriptions.set(activeSubscriptions);
          }),
          catchError((error) => {
            console.error('Ошибка загрузки подписок с ID:', error);
            this.userSubscriptions.set([]);
            return of([]);
          })
        );
      }),
      catchError((error) => {
        console.error('Ошибка получения сессии:', error);
        this.userSubscriptions.set([]);
        return of([]);
      })
    );
  }

  /**
   * Проверяет, подписан ли пользователь на конкретную подписку (subscriptionId)
   */
  isSubscribedToSubscription(subscriptionId: string): boolean {
    const subscriptions = this.userSubscriptions();
    return subscriptions.some(sub => sub.subscriptionId === subscriptionId);
  }

  /**
   * Получает список subscriptionId, на которые подписан пользователь
   */
  getSubscribedSubscriptionIds(): string[] {
    return this.userSubscriptions().map(sub => sub.subscriptionId);
  }
}
