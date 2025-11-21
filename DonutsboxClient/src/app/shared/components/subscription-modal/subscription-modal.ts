import { Component, inject, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { SubscriptionDto } from '@app/api/donutsbox/model/subscriptionDto';
import { UserInteractionService } from '@app/api/donutsbox/api/userInteraction.service';
import { UserSubscriptionCreateDto } from '@app/api/donutsbox/model/userSubscriptionCreateDto';

type SubscriptionPlan = {
  key: string;
  name: string | null;
  description: string | null;
  pictureURL?: string | null;
  monthlyPrice: string;
  options: SubscriptionDto[];
};

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
  readonly expandedPlanKey = signal<string | null>(null);

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';
  readonly defaultSubscriptionImage = 'https://via.placeholder.com/300x200?text=Subscription';
  private readonly currencyFormatter = new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  get subscriptionPlans(): SubscriptionPlan[] {
    const subscriptions = this.author?.subscriptions ?? [];
    const groups = new Map<string, SubscriptionPlan>();

    subscriptions.forEach((subscription) => {
      const key = this.getPlanKey(subscription);
      if (!groups.has(key)) {
        groups.set(key, {
          key,
          name: subscription.name,
          description: subscription.description,
          pictureURL: subscription.pictureURL,
          monthlyPrice: subscription.monthlyPrice || subscription.price || '0',
          options: []
        });
      }

      groups.get(key)!.options.push(subscription);
    });

    return Array.from(groups.values()).map((plan) => ({
      ...plan,
      options: plan.options.slice().sort((a, b) => {
        const periodA = a.subscriptionPeriodMonths ?? 0;
        const periodB = b.subscriptionPeriodMonths ?? 0;
        return periodA - periodB;
      })
    }));
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
    return this.formatCurrency(price);
  }

  formatMonthlyPrice(price?: string | null): string {
    const formatted = this.formatCurrency(price);
    return `${formatted} / мес`;
  }

  private formatCurrency(value?: string | null): string {
    if (!value) {
      return this.currencyFormatter.format(0);
    }

    const normalized = value.replace(',', '.');
    const amount = Number(normalized);
    if (Number.isNaN(amount)) {
      return `${value} ₽`;
    }

    return this.currencyFormatter.format(amount);
  }

  formatDuration(months?: number): string {
    if (!months || months === 1) return '1 месяц';
    if (months % 12 === 0) {
      const years = months / 12;
      return years === 1 ? '1 год' : `${years} года`;
    }
    return `${months} мес.`;
  }

  togglePlan(planKey: string): void {
    this.expandedPlanKey.update((current) => (current === planKey ? null : planKey));
  }

  isPlanExpanded(planKey: string): boolean {
    return this.expandedPlanKey() === planKey;
  }

  private getPlanKey(subscription: SubscriptionDto): string {
    return [
      subscription.name ?? '',
      subscription.description ?? '',
      subscription.pictureURL ?? ''
    ].join('|');
  }
}
