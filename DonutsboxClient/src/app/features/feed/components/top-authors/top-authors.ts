import { Component, inject, ChangeDetectionStrategy, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { FeedFacade } from '../../services/feed-facade';
import { SubscriptionModalService } from '@app/shared/services/subscription-modal.service';
import { Subscription } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-top-authors',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './top-authors.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopAuthors implements OnInit, OnDestroy {
  private readonly feedFacade = inject(FeedFacade);
  private readonly subscriptionModalService = inject(SubscriptionModalService);
  private subscriptionSuccessSub?: Subscription;
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly topAuthors = this.feedFacade.topAuthors;
  readonly loading = this.feedFacade.isLoadingTopAuthors;
  readonly error = this.feedFacade.topAuthorsError;
  readonly topAuthorsLoaded = this.feedFacade.topAuthorsLoaded;

  // подписанные URL через фасад
  readonly authorAvatarUrlMap = this.feedFacade.authorAvatarUrlMap;
  
  // Список ID авторов, на которых подписан пользователь
  readonly subscribedAuthorIds = this.feedFacade.userSubscribedAuthorIds;

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }
    this.loadTopAuthors();
    // Подписываемся на успешную подписку
    this.subscriptionSuccessSub = this.subscriptionModalService.subscriptionSuccess.subscribe(() => {
      this.loadTopAuthors();
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

  // Проверяет, подписан ли пользователь на автора
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

  loadTopAuthors(): void {
    if (!this.isBrowser) {
      return;
    }
    this.feedFacade.loadTopAuthors(10).subscribe();
  }
}
