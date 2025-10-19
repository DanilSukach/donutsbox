import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { TopAuthors } from '../../components/top-authors/top-authors';
import { PostsFeed } from '@app/shared/components/posts-feed/posts-feed';
import { FeedFacade } from '../../services/feed-facade';
import { UserProfileIcon } from '../../../../shared/components/user-profile-icon/user-profile-icon';

@Component({
  selector: 'app-feed-page',
  imports: [TopAuthors, UserProfileIcon, PostsFeed],
  templateUrl: './feed-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeedPage implements OnInit {
  private readonly feedFacade = inject(FeedFacade);
  
  readonly userGuid = this.feedFacade.userGuid;
  readonly isLoading = this.feedFacade.isLoadingUserData;

  // Функция для загрузки feed постов
  readonly loadFeedPosts = (page: number, pageSize: number) => {
    return this.feedFacade.getSubscriptionFeed(page, pageSize);
  };

  ngOnInit(): void {
    this.feedFacade.loadTopAuthors(10).subscribe();
  }
}
