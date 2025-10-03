import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { TopAuthors } from '../../components/top-authors/top-authors';
import { FeedFacade } from '../../services/feed-facade';
import { UserProfileIcon } from '../../../../shared/components/user-profile-icon/user-profile-icon';

@Component({
  selector: 'app-feed-page',
  imports: [TopAuthors, UserProfileIcon],
  templateUrl: './feed-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeedPage implements OnInit {
  private readonly feedFacade = inject(FeedFacade);
  
  // Используем signals из facade
  readonly userGuid = this.feedFacade.userGuid;
  readonly isLoading = this.feedFacade.isLoadingUserData;

  ngOnInit(): void {
    this.loadFeedData();
  }

  private loadFeedData(): void {
    // Загружаем персональный контент пользователя
    this.feedFacade.loadUserFeedContent().subscribe({
      next: (content) => {
        console.log('Персональный контент загружен:', content);
      },
      error: (err) => {
        console.error('Ошибка загрузки контента:', err);
      }
    });
  }
}
