import { Component, inject, input, signal } from '@angular/core';
import { PostsFacade } from '@app/features/profile/services/posts-facade';
import { VideoPlayer } from '@app/shared/components/video-player/video-player';
import { PostComments } from "@app/shared/components/post-comments/post-comments";

interface PostVideo {
  id: string;
  title: string;
  status: string;
  thumbnailUrl?: string | null;
  hlsUrl?: string | null;
}

interface Post {
  id: string;
  title?: string | null;
  text?: string | null;
  createdAt: string;
  publishedAt?: string | null;
  likesCount?: number;
  dislikesCount?: number;
  commentsCount?: number;
  videos?: PostVideo[];
  pictureUrls?: string[];
}

@Component({
  selector: 'app-post-card',
  imports: [VideoPlayer, PostComments],
  templateUrl: './post-card.html',
  styleUrl: './post-card.css',
})
export class PostCard {
  readonly post = input.required<Post>();
  readonly selectedVideoIndex = signal(0);
  readonly showComments = signal(false);
  readonly isOwner = input<boolean>(false); 
  readonly showDeleteModal = signal(false);

  private postsFacade = inject(PostsFacade);

  get currentVideo() {
    const videos = this.post().videos;
    if (!videos || videos.length === 0) return null;

    const video = videos[this.selectedVideoIndex()];

    return video;
  }

  selectVideo(index: number): void {
    this.selectedVideoIndex.set(index);
  }

  getVideoThumbnailUrl(videoId: string): string {
    return this.postsFacade.getVideoThumbnailUrl(videoId);
  }

  getVideoHlsUrl(videoId: string): string {
    return this.postsFacade.getVideoHlsUrl(videoId);
  }

  getPostImageUrl(imagePath: string): string {
    return this.postsFacade.getPostImageUrl(imagePath);
  }

  openDeleteModal(event: Event): void {
    event.stopPropagation();
    this.showDeleteModal.set(true);
  }
  
  closeDeleteModal(): void {
    this.showDeleteModal.set(false);
  }

  confirmDelete(): void {
    this.postsFacade.deletePost(this.post().id).subscribe({
      next: () => {
        console.log('Пост удален успешно:', this.post().id);
        this.closeDeleteModal();
      },
      error: (error) => {
        console.error('Ошибка удаления поста:', error);
        this.closeDeleteModal();
      }
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('ru-RU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  toggleComments(): void {
    this.showComments.update(show => !show);
  }
}

