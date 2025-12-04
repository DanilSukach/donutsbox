import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VideoStatusPollService } from '../../services/video-status-poll.service';

@Component({
  selector: 'app-video-processing-indicator',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (videoStatusPollService.isPolling()) {
      <div class="fixed bottom-4 right-4 z-50 bg-gradient-to-r from-amber-500 to-amber-600 text-white px-6 py-4 rounded-xl shadow-2xl flex items-center gap-3">
        <svg class="animate-spin h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <div>
          <p class="font-semibold">📝 Готовится новый пост...</p>
          <p class="text-xs opacity-90">Появится в ленте после обработки медиа (видео и аудио)</p>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: contents;
    }
  `]
})
export class VideoProcessingIndicator {
  readonly videoStatusPollService = inject(VideoStatusPollService);
}

