import { Component, inject, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { SubscriptionDto } from '@app/api/donutsbox/model/subscriptionDto';
import { UserInteractionService } from '@app/api/donutsbox/api/userInteraction.service';
import { UserSubscriptionCreateDto } from '@app/api/donutsbox/model/userSubscriptionCreateDto';

@Component({
  selector: 'app-subscription-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subscription-modal.html',
  styleUrl: './subscription-modal.css'
})
export class SubscriptionModal {
  private userInteractionService = inject(UserInteractionService);
  private router = inject(Router);

  @Input() author: AuthorRequestDto | null = null;
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() subscriptionSuccess = new EventEmitter<void>();

  readonly isSubscribing = signal(false);
  readonly subscriptionError = signal<string | null>(null);

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';
  readonly defaultSubscriptionImage = 'https://via.placeholder.com/300x200?text=Subscription';

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  close(): void {
    this.closeModal.emit();
  }

  subscribeToSubscription(subscription: SubscriptionDto): void {
    if (!subscription.id || this.isSubscribing()) return;

    this.isSubscribing.set(true);
    this.subscriptionError.set(null);

    const subscriptionData: UserSubscriptionCreateDto = {
      subscriptionId: subscription.id
    };

    this.userInteractionService.apiUserInteractionSubscribeUserPost(subscriptionData).subscribe({
      next: () => {
        this.isSubscribing.set(false);
        this.subscriptionSuccess.emit();
        this.close();
        
        // Переход на страницу автора после успешной подписки
        if (this.author?.id) {
          this.router.navigate(['/profile', this.author.id]);
        }
      },
      error: (error) => {
        console.error('Ошибка подписки:', error);
        this.isSubscribing.set(false);
        this.subscriptionError.set('Не удалось оформить подписку. Попробуйте позже.');
      }
    });
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.defaultAvatar;
  }

  onSubscriptionImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.style.display = 'none';
  }

  formatPrice(price?: string | null): string {
    if (!price) return '0 ₽';
    return `${price} ₽`;
  }
}
