import { inject, Injectable, signal } from '@angular/core';
import { AuthorPreviewDto, UserDataService, UserInteractionService, UserSubscriptionService, UserSubscriptionDto } from '@app/api/donutsbox';
import { catchError, Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserSubscriptionsFacade {
  private readonly userDataService = inject(UserDataService);
  private readonly userInteraction = inject(UserInteractionService);
  private readonly userSubscriptionService = inject(UserSubscriptionService);

  readonly subscriptions = signal<AuthorPreviewDto[]>([]);
  readonly userSubscriptions = signal<UserSubscriptionDto[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  loadUserSubscriptions(): Observable<AuthorPreviewDto[]> {
    this.isLoading.set(true);
    this.error.set(null);

    return this.userDataService.apiUserDataSubscriptionsGet().pipe(
      tap((subscriptions) => {
        console.log('Подписки пользователя загружены:', subscriptions);
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
   */
  loadUserSubscriptionsWithIds(): Observable<UserSubscriptionDto[]> {
    return this.userSubscriptionService.apiUserSubscriptionGet().pipe(
      tap((subscriptions) => {
        console.log('Подписки пользователя с ID загружены:', subscriptions);
        // Фильтруем только активные подписки
        const now = new Date();
        const activeSubscriptions = subscriptions.filter(sub => {
          const endDate = new Date(sub.endDate);
          return sub.status?.toLowerCase() === 'active' && endDate >= now;
        });
        this.userSubscriptions.set(activeSubscriptions);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки подписок с ID:', error);
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
