import { CommonModule } from '@angular/common';
import { Component, inject, input, output, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SubscriptionDto } from '@app/api/donutsbox';
import { ProfileFacade } from '@app/features/profile/services/profile-facade';

@Component({
  selector: 'app-edit-subscription-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './edit-subscription-modal.html',
  styleUrl: './edit-subscription-modal.css'
})
export class EditSubscriptionModal implements OnInit {
  private fb = inject(FormBuilder);
  private profileFacade = inject(ProfileFacade);

  subscription: SubscriptionDto | null = null;
  readonly closeModal = output<void>();
  readonly subscriptionUpdated = output<void>();

  readonly isUpdating = this.profileFacade.isUpdatingSubscription;
  readonly errorMessage = this.profileFacade.subscriptionError;

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(30)]],
    description: ['', [Validators.required]],
    monthlyPrice: ['', [Validators.required, Validators.min(0.01)]]
  });

  ngOnInit(): void {
    if (this.subscription) {
      this.form.patchValue({
        name: this.subscription.name || '',
        description: this.subscription.description || '',
        monthlyPrice: this.subscription.monthlyPrice || this.subscription.price || ''
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.value;
    const price = parseFloat(value.monthlyPrice || '0');
    
    if (isNaN(price) || price <= 0) {
      return;
    }

    if (!this.subscription?.id) return;
    
    this.profileFacade.updateSubscription(this.subscription.id, {
      name: value.name ?? '',
      description: value.description ?? '',
      price: price.toString()
    }).subscribe({
      next: (success) => {
        if (success) {
          this.subscriptionUpdated.emit();
          this.closeModal.emit();
        }
      },
      error: () => {
        // Ошибка уже обрабатывается через errorMessage signal
      }
    });
  }

  onClose(): void {
    this.profileFacade.clearErrors();
    this.closeModal.emit();
  }
}

