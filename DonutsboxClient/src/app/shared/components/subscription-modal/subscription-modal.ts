import { Component, inject, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { Router } from '@angular/router';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { SubscriptionDto } from '@app/api/donutsbox/model/subscriptionDto';
import { SubscriptionPaymentsService } from '@app/api/donutsbox/api/subscriptionPayments.service';
import { SubscriptionPaymentRequestDto } from '@app/api/donutsbox/model/subscriptionPaymentRequestDto';
import { SubscriptionPaymentResponseDto } from '@app/api/donutsbox/model/subscriptionPaymentResponseDto';
import { FilesService } from '@app/api/donutsbox/api/files.service';

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
  private router = inject(Router);
  private paymentsService = inject(SubscriptionPaymentsService);
  private document = inject(DOCUMENT);
  private filesService = inject(FilesService);

  private _author: AuthorRequestDto | null = null;
  
  @Input() 
  set author(value: AuthorRequestDto | null) {
    this._author = value;
    this.loadAuthorAvatar();
  }
  get author(): AuthorRequestDto | null {
    return this._author;
  }
  
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() subscriptionSuccess = new EventEmitter<void>();

  readonly isSubscribing = signal(false);
  readonly subscriptionError = signal<string | null>(null);
  readonly expandedPlanKey = signal<string | null>(null);
  readonly authorAvatarUrl = signal<string | null>(null);

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';
  readonly defaultSubscriptionImage = 'https://via.placeholder.com/300x200?text=Subscription';

  private loadAuthorAvatar(): void {
    const author = this._author;
    if (author?.avatarUrl) {
      this.filesService.apiFilesImagesUrlGet(author.avatarUrl, 300).subscribe({
        next: (response) => {
          this.authorAvatarUrl.set(response.url ?? null);
        },
        error: () => {
          this.authorAvatarUrl.set(null);
        }
      });
    } else {
      this.authorAvatarUrl.set(null);
    }
  }
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

    const request: SubscriptionPaymentRequestDto = {
      subscriptionId: subscription.id,
      returnUrl: this.buildReturnUrl()
    };

    this.paymentsService.apiPaymentsSubscriptionsPost(request).subscribe({
      next: (response) => this.handlePaymentCreated(response),
      error: (error) => {
        console.error('Ошибка создания платежа YooKassa:', error);
        this.isSubscribing.set(false);
        const message = error?.error?.message || 'Не удалось создать платеж. Попробуйте позже.';
        this.subscriptionError.set(message);
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

  private buildReturnUrl(): string {
    const origin = this.document?.location?.origin ?? window.location.origin;
    return `${origin}/payments/result`;
  }

  private handlePaymentCreated(response: SubscriptionPaymentResponseDto): void {
    this.isSubscribing.set(false);

    const confirmationUrl = response.confirmationUrl ?? null;
    const paymentRequestId = response.paymentRequestId ?? null;

    if (!confirmationUrl && !paymentRequestId) {
      this.subscriptionError.set('Не удалось получить ссылку на оплату. Попробуйте позже.');
      return;
    }

    this.subscriptionSuccess.emit();
    this.close();

    if (confirmationUrl) {
      this.document.location.href = confirmationUrl;
      return;
    }

    // Если YooKassa не вернула ссылку, перенаправляем на страницу статуса платежа.
    void this.router.navigate(['/payments/result'], {
      queryParams: { paymentRequestId }
    });
  }
}
