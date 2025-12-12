import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthorPreviewDto } from '@app/api/donutsbox/model/authorPreviewDto';
import { UserSubscriptionsFacade } from '../../services/user-subscriptions-facade';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-user-subscriptions',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './user-subscriptions.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserSubscriptions implements OnInit {
  private userSubscriptionsFacade = inject(UserSubscriptionsFacade);
  private router = inject(Router);

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';

  // Состояние модального окна
  readonly showUnsubscribeModal = signal(false);
  readonly selectedAuthor = signal<AuthorPreviewDto | null>(null);

  ngOnInit(): void {
    this.loadSubscriptions();
  }

  loadSubscriptions(): void {
    this.userSubscriptionsFacade.loadUserSubscriptions().subscribe();
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.defaultAvatar;
  }

  trackByAuthorId(index: number, author: AuthorPreviewDto): string {
    return author.id || index.toString();
  }

  // Навигация на страницу автора
  navigateToAuthor(author: AuthorPreviewDto): void {
    if (author.id) {
      this.router.navigate(['/profile', author.id]);
    }
  }

  // Открытие модального окна отписки
  openUnsubscribeModal(author: AuthorPreviewDto, event: Event): void {
    event.stopPropagation();
    this.selectedAuthor.set(author);
    this.showUnsubscribeModal.set(true);
  }

  // Закрытие модального окна
  closeUnsubscribeModal(): void {
    this.showUnsubscribeModal.set(false);
    this.selectedAuthor.set(null);
  }

  // Подтверждение отписки
  confirmUnsubscribe(): void {
    const author = this.selectedAuthor();
    if (author && author.id) {
      this.userSubscriptionsFacade.unsubscribeFromCreator(author.id).subscribe({
        next: () => {
          console.log('Отписка от создателя успешна:', author);
          this.closeUnsubscribeModal();
        },
        error: (error) => {
          console.error('Ошибка отписки от создателя:', error);
        }
      });
    }
  }

  // Геттеры для доступа к состоянию facade
  get subscriptions() {
    return this.userSubscriptionsFacade.subscriptions;
  }

  get isLoading() {
    return this.userSubscriptionsFacade.isLoading;
  }

  get error() {
    return this.userSubscriptionsFacade.error;
  }
}
