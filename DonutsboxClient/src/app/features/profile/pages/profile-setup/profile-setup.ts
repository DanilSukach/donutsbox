import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreatorPageDataDto } from '@app/api/donutsbox';
import { ProfileFacade } from '../../services/profile-facade';

@Component({
  selector: 'app-profile-setup',
  imports: [ReactiveFormsModule],
  templateUrl: './profile-setup.html',
  styleUrl: './profile-setup.css'
})
export class ProfileSetup {
  private fb = inject(FormBuilder);
  private profileFacade = inject(ProfileFacade);

  setupForm!: FormGroup;
  
  readonly isCreating = this.profileFacade.isCreatingProfile;
  readonly errorMessage = this.profileFacade.profileError;

  ngOnInit(): void {
    this.setupForm = this.fb.group({
      pageName: ['', [Validators.required, Validators.maxLength(40)]],
      description: [''],
      avatarUrl: [null],
      bannerUrl: [null],
    });
  }

  onSubmit(): void {
    if (this.setupForm.invalid) {
      return;
    }

    const creatorData: CreatorPageDataDto = {
      pageName: this.setupForm.value.pageName,
      description: this.setupForm.value.description,
      avatarUrl: this.setupForm.value.avatarUrl,
      bannerUrl: this.setupForm.value.bannerUrl,
      subscribersCount: 0,
    };

    this.profileFacade.createCreatorPage(creatorData).subscribe();
  }
}
