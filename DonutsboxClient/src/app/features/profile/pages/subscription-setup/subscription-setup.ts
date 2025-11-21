import { Component, inject } from '@angular/core';
import { SubscriptionCreateDto } from '@app/api/donutsbox';
import { SubscriptionForm } from '@app/shared/components/subscription-form/subscription-form';
import { ProfileFacade } from '../../services/profile-facade';

@Component({
  selector: 'app-subscription-setup',
  imports: [SubscriptionForm],
  templateUrl: './subscription-setup.html',
  styleUrl: './subscription-setup.css'
})
export class SubscriptionSetup {
  private profileFacade = inject(ProfileFacade);

  readonly isCreating = this.profileFacade.isCreatingSubscription;
  readonly errorMessage = this.profileFacade.subscriptionError;

  onSubmit(subscriptionData: SubscriptionCreateDto): void {
    this.profileFacade.createSubscription(subscriptionData).subscribe();
  }

  skipSubscription(): void {
    this.profileFacade.skipSubscription();
  }
}
