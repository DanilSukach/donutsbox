import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubscriptionDto } from '@app/api/donutsbox';
import { PostsFacade } from '../../services/posts-facade';
import { catchError, Observable, of, tap } from 'rxjs';
import { EditSubscriptionModalService } from '@app/shared/services/edit-subscription-modal.service';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-creator-subscriptions',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule
  ],
  templateUrl: './creator-subscriptions.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreatorSubscriptions implements OnInit {
  private postsFacade = inject(PostsFacade);
  private editSubscriptionModalService = inject(EditSubscriptionModalService);

  readonly subscriptions = signal<SubscriptionDto[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isExpanded = signal(false);
  readonly expandedPlanKeys = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.loadSubscriptions();
    
    // Подписываемся на обновление подписки
    this.editSubscriptionModalService.subscriptionUpdated.subscribe(() => {
      this.loadSubscriptions();
    });
  }

  loadSubscriptions(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.postsFacade.getCreatorSubscriptions().pipe(
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
    ).subscribe();
  }

  openEditModal(subscription: SubscriptionDto): void {
    this.editSubscriptionModalService.open(subscription);
  }

  toggleExpanded(): void {
    this.isExpanded.set(!this.isExpanded());
  }

  formatPrice(price: string | null | undefined): string {
    if (!price) return '0 ₽';
    const numPrice = parseFloat(price);
    if (isNaN(numPrice)) return '0 ₽';
    return `${numPrice.toFixed(2)} ₽`;
  }

  getPlanKey(subscription: SubscriptionDto): string {
    return subscription.name || subscription.id || '';
  }

  get subscriptionPlans(): Array<{
    key: string;
    name: string | null;
    description: string | null;
    monthlyPrice: string;
    options: SubscriptionDto[];
  }> {
    const allSubscriptions = this.subscriptions();
    const groups = new Map<string, {
      key: string;
      name: string | null;
      description: string | null;
      monthlyPrice: string;
      options: SubscriptionDto[];
    }>();

    allSubscriptions.forEach((subscription) => {
      const key = this.getPlanKey(subscription);
      if (!groups.has(key)) {
        groups.set(key, {
          key,
          name: subscription.name,
          description: subscription.description,
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

  isPlanExpanded(planKey: string): boolean {
    return this.expandedPlanKeys().has(planKey);
  }

  togglePlan(planKey: string): void {
    const expanded = new Set(this.expandedPlanKeys());
    if (expanded.has(planKey)) {
      expanded.delete(planKey);
    } else {
      expanded.add(planKey);
    }
    this.expandedPlanKeys.set(expanded);
  }

  formatDuration(months: number | null | undefined): string {
    if (!months) return '';
    if (months === 1) return '1 месяц';
    if (months < 5) return `${months} месяца`;
    return `${months} месяцев`;
  }
}

