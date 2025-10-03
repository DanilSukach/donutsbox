import { Component, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { FeedFacade } from '../../services/feed-facade';
import { SubscriptionModal } from '@app/shared/components/subscription-modal/subscription-modal';

@Component({
  selector: 'app-top-authors',
  standalone: true,
  imports: [SubscriptionModal],
  templateUrl: './top-authors.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopAuthors {
  private readonly feedFacade = inject(FeedFacade);
  
  // Используем signals из facade
  readonly topAuthors = this.feedFacade.topAuthors;
  readonly loading = this.feedFacade.isLoadingTopAuthors;
  readonly error = this.feedFacade.topAuthorsError;

  readonly selectedAuthor = signal<AuthorRequestDto | null>(null);
  readonly isModalOpen = signal(false);

  constructor() {
    this.loadTopAuthors();
  }

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';

  formatSubscribersCount(count?: number): string {
    if (!count) return '0';
    
    if (count >= 1000000) {
      return (count / 1000000).toFixed(1) + 'M';
    } else if (count >= 1000) {
      return (count / 1000).toFixed(1) + 'K';
    }
    
    return count.toString();
  }

  onAuthorClick(author: AuthorRequestDto): void {
    if (author.id) {
      this.feedFacade.navigateToAuthor(author.id);
    }
  }

  onSubscribeClick(author: AuthorRequestDto, event: Event): void {
    event.stopPropagation();
    
    this.selectedAuthor.set(author);
    this.isModalOpen.set(true);
  }

  onModalClose(): void {
    this.isModalOpen.set(false);
    this.selectedAuthor.set(null);
  }

  onSubscriptionSuccess(): void {
    console.log('Успешная подписка на автора:', this.selectedAuthor()?.pageName);
    // Можно обновить список авторов или показать уведомление
    this.loadTopAuthors();
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.defaultAvatar;
  }

  trackByAuthorId(index: number, author: AuthorRequestDto): string {
    return author.id || index.toString();
  }

  loadTopAuthors(): void {
    this.feedFacade.loadTopAuthors(10).subscribe();
  }
}
