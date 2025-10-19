import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PostsRefresh {
  readonly refreshTrigger = signal(0);

  triggerRefresh(): void {
    this.refreshTrigger.update(v => v + 1);
  }
}

