import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PostsRefresh {
  readonly refreshTrigger = signal(0);
  private postPublished$ = new Subject<string>();
  readonly postPublished = this.postPublished$.asObservable(); // Событие публикации поста

  triggerRefresh(): void {
    this.refreshTrigger.update(v => v + 1);
  }

  notifyPostPublished(postId: string): void {
    console.log('📢 Уведомление о публикации поста:', postId);
    this.postPublished$.next(postId);
    this.triggerRefresh();
  }
}

