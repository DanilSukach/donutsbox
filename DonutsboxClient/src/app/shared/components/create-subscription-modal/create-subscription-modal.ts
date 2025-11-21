import { CommonModule } from '@angular/common';
import { Component, inject, output } from '@angular/core';
import { SubscriptionCreateDto } from '@app/api/donutsbox';
import { SubscriptionForm } from '../subscription-form/subscription-form';
import { ProfileFacade } from '@app/features/profile/services/profile-facade';

@Component({
  selector: 'app-create-subscription-modal',
  standalone: true,
  imports: [CommonModule, SubscriptionForm],
  templateUrl: './create-subscription-modal.html',
  styleUrl: './create-subscription-modal.css'
})
export class CreateSubscriptionModal {
  private profileFacade = inject(ProfileFacade);

  readonly closeModal = output<void>();
  readonly subscriptionCreated = output<void>();

  readonly isCreating = this.profileFacade.isCreatingSubscription;
  readonly errorMessage = this.profileFacade.subscriptionError;

  onSubmit(payload: SubscriptionCreateDto): void {
    this.profileFacade
      .createSubscription(payload, { navigateOnSuccess: false })
      .subscribe((result) => {
        if (result) {
          this.subscriptionCreated.emit();
        }
      });
  }

  onClose(): void {
    this.profileFacade.clearErrors();
    this.closeModal.emit();
  }
}

