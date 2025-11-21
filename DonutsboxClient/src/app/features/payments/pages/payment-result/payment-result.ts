import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { SubscriptionPaymentsService } from '@app/api/donutsbox/api/subscriptionPayments.service';
import { SubscriptionPaymentStatusDto } from '@app/api/donutsbox/model/subscriptionPaymentStatusDto';

type ViewState = 'loading' | 'success' | 'processing' | 'error';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-result.html',
  styleUrl: './payment-result.css'
})
export class PaymentResult implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly paymentsService = inject(SubscriptionPaymentsService);

  private routeSub?: Subscription;
  private lastPaymentRequestId: string | null = null;

  readonly state = signal<ViewState>('loading');
  readonly statusDetails = signal<SubscriptionPaymentStatusDto | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.routeSub = this.route.queryParamMap.subscribe((params) => {
      const paymentRequestId = params.get('paymentRequestId');
      if (!paymentRequestId) {
        this.state.set('error');
        this.errorMessage.set('Отсутствует идентификатор платежа.');
        return;
      }
      this.loadStatus(paymentRequestId);
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  private loadStatus(paymentRequestId: string): void {
    this.state.set('loading');
    this.errorMessage.set(null);
    this.statusDetails.set(null);
    this.lastPaymentRequestId = paymentRequestId;

    this.paymentsService.apiPaymentsSubscriptionsPaymentRequestIdGet(paymentRequestId).subscribe({
        next: (status) => {
          this.statusDetails.set(status);
          this.state.set(this.resolveViewState(status));
        },
        error: (error) => {
          console.error('Не удалось получить статус платежа', error);
          this.state.set('error');
          this.errorMessage.set('Не удалось получить статус платежа. Попробуйте обновить страницу позже.');
        }
      });
  }

  private resolveViewState(status: SubscriptionPaymentStatusDto): ViewState {
    const normalizedStatus = (status.status ?? '').toLowerCase();
    if (normalizedStatus === 'succeeded') {
      return 'success';
    }
    if (normalizedStatus === 'pending' || normalizedStatus === 'waiting_for_capture') {
      return 'processing';
    }
    return 'error';
  }

  protected retry(): void {
    if (!this.lastPaymentRequestId) {
      return;
    }
    this.loadStatus(this.lastPaymentRequestId);
  }

  protected formatDate(value?: string | null): string {
    if (!value) {
      return '';
    }

    try {
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) {
        return value;
      }
      return new Intl.DateTimeFormat('ru-RU', {
        day: 'numeric',
        month: 'long',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      }).format(date);
    } catch {
      return value;
    }
  }
}

