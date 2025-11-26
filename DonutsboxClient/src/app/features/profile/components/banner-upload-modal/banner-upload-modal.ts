import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-banner-upload-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './banner-upload-modal.html',
  styleUrl: './banner-upload-modal.css'
})
export class BannerUploadModal {
  @Output() closed = new EventEmitter<void>();
  @Output() uploaded = new EventEmitter<File>();

  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);
  readonly isUploading = signal(false);
  readonly error = signal<string | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.error.set('Пожалуйста, выберите изображение');
      return;
    }

    if (file.size > 20 * 1024 * 1024) {
      this.error.set('Файл слишком большой (максимум 20 МБ)');
      return;
    }

    this.error.set(null);
    this.selectedFile.set(file);

    const reader = new FileReader();
    reader.onload = (e) => {
      this.previewUrl.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  }

  onUpload(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.isUploading.set(true);
    this.uploaded.emit(file);
  }

  onClose(): void {
    if (this.isUploading()) return;
    this.closed.emit();
  }

  clearSelection(): void {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
    this.error.set(null);
  }

  setUploading(value: boolean): void {
    this.isUploading.set(value);
  }

  setError(message: string): void {
    this.error.set(message);
    this.isUploading.set(false);
  }
}

