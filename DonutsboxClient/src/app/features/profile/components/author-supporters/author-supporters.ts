import { Component, inject, Input, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthorsService } from '@app/api/donutsbox/api/authors.service';
import { UserRequestDto } from '@app/api/donutsbox/model/userRequestDto';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';

@Component({
  selector: 'app-author-supporters',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './author-supporters.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthorSupporters implements OnInit {
  private authorsService = inject(AuthorsService);

  @Input() authorId?: string;
  @Input() count: number = 5;

  readonly supporters = signal<UserRequestDto[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';

  ngOnInit(): void {
    this.loadSupporters();
  }

  loadSupporters(): void {
    if (!this.authorId) return;

    this.isLoading.set(true);
    this.error.set(null);

    // Используем новый API endpoint для получения поддерживающих подписчиков
    this.authorsService.apiAuthorsTopSupportedGet(this.count).subscribe({
      next: (supporters) => {
        console.log('Поддерживающие подписчики:', supporters);
        this.supporters.set(supporters);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Ошибка загрузки поддерживающих подписчиков:', error);
        this.error.set('Не удалось загрузить список поддерживающих');
        this.isLoading.set(false);
      }
    });
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.defaultAvatar;
  }

  trackByUserId(index: number, user: UserRequestDto): string {
    return user.id || index.toString();
  }
}
