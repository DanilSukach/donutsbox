import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PostCommentDto } from '@app/api/donutsbox';
import { CommentsFacade } from '@app/core/services/comments-facade';
import { SessionService } from '@app/core/services/session.service';
import { Subscription } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-post-comments',
  imports: [FormsModule, LucideAngularModule],
  templateUrl: './post-comments.html',
  styleUrl: './post-comments.css'
})
export class PostComments {
   readonly postId = input.required<string>();

  private commentsFacade = inject(CommentsFacade);
  private sessionService = inject(SessionService);

  comments = signal<PostCommentDto[]>([]);
  newCommentText = signal('');
  editingCommentId = signal<string | null>(null);
  editingText = signal('');
  isLoading = signal(false);
  currentUserId = signal<string | null>(null);

  private subscriptions: Subscription[] = [];

  constructor() {
    this.sessionService.ensureSession().subscribe();
    effect(() => {
      const session = this.sessionService.session();
      this.currentUserId.set(session?.userId ?? null);
    });

    effect(() => {
      const postId = this.postId();
      if (postId) {
        this.subscribeToPost(postId);
        this.loadComments();
      }
    });
  }

private subscribeToPost(postId: string): void {
  console.log(`📋 [PostComments] Subscribing to post: ${postId}`);
  this.unsubscribe();
  
  // Присоединяемся к комнате
  this.commentsFacade.joinPostComments(postId);

  // CommentAdded
  this.subscriptions.push(
    this.commentsFacade.commentAdded$.subscribe(comment => {
      console.log(`[PostComments] Received commentAdded event:`, comment);
      if (comment.postId === postId) {
        console.log(`✅ [PostComments] Adding comment to UI for post ${postId}`);
        this.comments.update(list => [...list, comment]);
      } else {
        console.log(`⚠️ [PostComments] Comment is for different post (${comment.postId}), ignoring`);
      }
    })
  );

  // CommentUpdated
  this.subscriptions.push(
    this.commentsFacade.commentUpdated$.subscribe(({ id, text }) => {
      console.log(`[PostComments] Received commentUpdated event:`, id, text);
      this.comments.update(list =>
        list.map(c => (c.id === id ? { ...c, text } : c))
      );
    })
  );

  // CommentDeleted
  this.subscriptions.push(
    this.commentsFacade.commentDeleted$.subscribe(commentId => {
      console.log(`[PostComments] Received commentDeleted event:`, commentId);
      this.comments.update(list => list.filter(c => c.id !== commentId));
    })
  );
}

  private unsubscribe(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.subscriptions = [];
  }

  loadComments(): void {
    this.isLoading.set(true);
    this.commentsFacade.getPostComments(this.postId()).subscribe({
      next: (comments) => {
        this.comments.set(comments);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        alert('Не удалось загрузить комментарии');
      }
    });
  }

  // 🔥 Просто отправляем - ответ приходит через SignalR событие
  addComment(): void {
    const text = this.newCommentText().trim();
    if (!text) return;

    this.isLoading.set(true);
    this.commentsFacade.addComment(this.postId(), text).subscribe({
      next: () => {
        // ✅ Просто очищаем поле. Новый комментарий придёт через SignalR
        this.newCommentText.set('');
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error adding comment:', error);
        this.isLoading.set(false);
        alert('Не удалось добавить комментарий');
      }
    });
  }

  startEdit(comment: PostCommentDto): void {
    this.editingCommentId.set(comment.id);
    this.editingText.set(comment.text || '');
  }

  cancelEdit(): void {
    this.editingCommentId.set(null);
    this.editingText.set('');
  }

  saveEdit(commentId: string): void {
    const text = this.editingText().trim();
    if (!text) return;

    this.isLoading.set(true);
    this.commentsFacade.updateComment(commentId, text).subscribe({
      next: () => {
        this.cancelEdit();
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.cancelEdit();
      }
    });
  }

  deleteComment(commentId: string): void {
    this.isLoading.set(true);
    this.commentsFacade.deleteComment(commentId).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  isOwner(comment: PostCommentDto): boolean {
    return comment.userId === this.currentUserId();
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'только что';
    if (diffMins < 60) return `${diffMins} мин назад`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)} ч назад`;

    return date.toLocaleDateString('ru-RU', {
      day: 'numeric',
      month: 'short'
    });
  }

  ngOnDestroy(): void {
    this.commentsFacade.leavePostComments(this.postId());
    this.unsubscribe();
  }
}
