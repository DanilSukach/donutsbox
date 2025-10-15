import { Component, inject, input, signal } from '@angular/core';
import { PostsFacade } from '../../services/posts-facade';
import { VideoPlayer } from '@app/shared/components/video-player/video-player';

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
  imports: [VideoPlayer],
  templateUrl: './post-card.html',
  styleUrl: './post-card.css',
})
export class PostCard {
  readonly post = input.required<Post>();
  readonly selectedVideoIndex = signal(0);

  private postsFacade = inject(PostsFacade);

  get currentVideo() {
    const videos = this.post().videos;
    if (!videos || videos.length === 0) return null;

    const video = videos[this.selectedVideoIndex()];

    console.log('Current video:', video);
    console.log('HLS URL from backend:', video.hlsUrl);
    console.log('HLS URL from facade:', this.getVideoHlsUrl(video.id));

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

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('ru-RU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}
