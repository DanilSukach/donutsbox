import { Component, inject, ChangeDetectionStrategy, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { FeedFacade } from '../../services/feed-facade';
import { SubscriptionModalService } from '@app/shared/services/subscription-modal.service';
import { Subscription } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-author-search',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './author-search.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthorSearch implements OnInit, OnDestroy {
  private readonly feedFacade = inject(FeedFacade);
  private readonly subscriptionModalService = inject(SubscriptionModalService);
  private subscriptionSuccessSub?: Subscription;

  readonly searchQuery = signal<string>('');
  readonly searchResults = this.feedFacade.searchResults;
  readonly isLoadingSearch = this.feedFacade.isLoadingSearch;
  readonly searchError = this.feedFacade.searchError;
  readonly authorAvatarUrlMap = this.feedFacade.authorAvatarUrlMap;
  readonly subscribedAuthorIds = this.feedFacade.userSubscribedAuthorIds;

  ngOnInit(): void {
    // Подписываемся на успешную подписку
    this.subscriptionSuccessSub = this.subscriptionModalService.subscriptionSuccess.subscribe(() => {
      this.feedFacade.loadUserSubscriptions();
    });
  }

  ngOnDestroy(): void {
    this.subscriptionSuccessSub?.unsubscribe();
  }

  readonly defaultAvatar = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiNFOUVDRUYiLz4KPHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeD0iOCIgeT0iOCI+CjxwYXRoIGQ9Ik0xMiAxMkM5Ljc5IDEyIDggMTAuMjEgOCA4UzkuNzkgNCA0IDRTMTYgNS43OSAxNiA4UzE0LjIxIDEyIDEyIDEyWk0xMiAxNEMxNi40MiAxNCAyMCAxNS43OSAyMCAxOFYyMEg0VjE4QzQgMTUuNzkgNy41OCAxNCAxMiAxNFoiIGZpbGw9IiM2Qzc1N0QiLz4KPC9zdmc+Cjwvc3ZnPgo=';

  formatSubscribersCount(count?: number): string {
    if (!count) return '0';
    if (count >= 1000000) return (count / 1000000).toFixed(1) + 'M';
    if (count >= 1000) return (count / 1000).toFixed(1) + 'K';
    return count.toString();
  }

  isSubscribed(author: AuthorRequestDto): boolean {
    return this.subscribedAuthorIds().has(author.id);
  }

  onAuthorClick(author: AuthorRequestDto): void {
    if (author.id) this.feedFacade.navigateToAuthor(author.id);
  }

  onSubscribeClick(author: AuthorRequestDto, event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.subscriptionModalService.open(author);
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.defaultAvatar;
  }

  trackByAuthorId(index: number, author: AuthorRequestDto): string {
    return author.id || index.toString();
  }

  onSearch(): void {
    const query = this.searchQuery().trim();
    if (query) {
      this.feedFacade.searchAuthors(query).subscribe();
    }
  }

  onClearSearch(): void {
    this.searchQuery.set('');
    this.feedFacade.clearSearch();
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }
}

