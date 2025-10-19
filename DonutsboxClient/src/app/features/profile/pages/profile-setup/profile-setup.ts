import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreatorPageDataDto } from '@app/api/donutsbox';
import { ProfileFacade } from '../../services/profile-facade';
import { firstValueFrom } from 'rxjs';

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

  // + локальные файлы
  private avatarFile: File | null = null;
  private bannerFile: File | null = null;

  // + ошибки загрузки изображений
  readonly imageUploadError = this.profileFacade.imageUploadError;
  readonly isUploadingAvatar = this.profileFacade.isUploadingAvatar;
  readonly isUploadingBanner = this.profileFacade.isUploadingBanner;

  ngOnInit(): void {
    this.setupForm = this.fb.group({
      pageName: ['', [Validators.required, Validators.maxLength(40)]],
      description: [''],
      avatarUrl: [null], // сюда положим objectKey после upload
      bannerUrl: [null],
    });
  }

  onAvatarSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.avatarFile = input.files?.[0] ?? null;
  }

  onBannerSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.bannerFile = input.files?.[0] ?? null;
  }

  async onSubmit(): Promise<void> {
    if (this.setupForm.invalid) return;

    // 1) Загружаем выбранные картинки через фасад (backend), сохраняем objectKey в форму
    if (this.avatarFile) {
      const avatarKey = await firstValueFrom(this.profileFacade.uploadAvatar(this.avatarFile));
      if (!avatarKey) return; // ошибка уже в imageUploadError
      this.setupForm.patchValue({ avatarUrl: avatarKey });
    }

    if (this.bannerFile) {
      const bannerKey = await firstValueFrom(this.profileFacade.uploadBanner(this.bannerFile));
      if (!bannerKey) return; // ошибка уже в imageUploadError
      this.setupForm.patchValue({ bannerUrl: bannerKey });
    }

    // 2) Вызываем создание страницы
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