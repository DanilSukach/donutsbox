import { inject, Injectable, signal } from '@angular/core';
import { AuthorPreviewDto, UserDataService, UserInteractionService } from '@app/api/donutsbox';
import { catchError, Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserSubscriptionsFacade {
  private readonly userDataService = inject(UserDataService);
  private readonly userInteraction = inject(UserInteractionService)

  readonly subscriptions = signal<AuthorPreviewDto[]>([]);
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
}
