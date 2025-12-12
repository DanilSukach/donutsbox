import { CommonModule } from '@angular/common';
import { Component, inject, input, output, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SubscriptionCreateDto } from '@app/api/donutsbox';

@Component({
  selector: 'app-subscription-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './subscription-form.html',
  styleUrl: './subscription-form.css'
})
export class SubscriptionForm implements OnInit {
  private fb = inject(FormBuilder);

  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly showSkip = input(false);
  readonly submitLabel = input('Создать подписку');
  readonly skipLabel = input('Пропустить');
  readonly initialName = input<string | null>(null);
  readonly initialDescription = input<string | null>(null);
  readonly initialPrice = input<string | null>(null);

  readonly submitted = output<SubscriptionCreateDto>();
  readonly skipped = output<void>();

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(30)]],
    description: ['', [Validators.required]],
    monthlyPrice: ['', [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    // Заполняем форму начальными значениями, если они переданы
    if (this.initialName() || this.initialDescription() || this.initialPrice()) {
      this.form.patchValue({
        name: this.initialName() || '',
        description: this.initialDescription() || '',
        monthlyPrice: this.initialPrice() || ''
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.value;
    const priceControl = value.monthlyPrice;
    const payload: SubscriptionCreateDto = {
      name: value.name ?? '',
      description: value.description ?? '',
      price: priceControl !== undefined && priceControl !== null ? priceControl.toString() : '0',
      pictureURL: null
    };

    this.submitted.emit(payload);
  }

  onSkip(): void {
    this.skipped.emit();
  }
}

