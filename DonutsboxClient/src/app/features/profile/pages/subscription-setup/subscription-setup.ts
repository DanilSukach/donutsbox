import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { ProfileFacade } from '../../services/profile-facade';
import { SubscriptionCreateDto, SubscriptionDto } from '@app/api/donutsbox';

@Component({
  selector: 'app-subscription-setup',
  imports: [ReactiveFormsModule],
  templateUrl: './subscription-setup.html',
  styleUrl: './subscription-setup.css'
})
export class SubscriptionSetup implements OnInit {
  private fb = inject(FormBuilder);
  private profileFacade = inject(ProfileFacade);

  subscriptionForm!: FormGroup;
  
  readonly isCreating = this.profileFacade.isCreatingSubscription;
  readonly errorMessage = this.profileFacade.subscriptionError;

  ngOnInit(): void {
    this.subscriptionForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(30)]],
      description: ['', [Validators.required]],
      monthlyPrice: ['', [Validators.required, Validators.min(0.01)]],
      pictureURL: ['']
    });
  }

  onSubmit(): void {
    if (this.subscriptionForm.invalid) {
      return;
    }

    const subscriptionData: SubscriptionCreateDto = {
      name: this.subscriptionForm.value.name,
      description: this.subscriptionForm.value.description,
      price: this.subscriptionForm.value.monthlyPrice.toString(),
      pictureURL: this.subscriptionForm.value.pictureURL,
    };

    this.profileFacade.createSubscription(subscriptionData).subscribe();
  }

  skipSubscription(): void {
    this.profileFacade.skipSubscription();
  }
}
