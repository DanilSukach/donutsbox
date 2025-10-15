// DonutsboxClient/src/app/shared/components/video-player/video-player.ts
import { 
  Component, 
  input,
  CUSTOM_ELEMENTS_SCHEMA,
  effect
} from '@angular/core';

@Component({
  selector: 'app-video-player',
  standalone: true,
  imports: [],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './video-player.html',
  styleUrl: './video-player.css'
})
export class VideoPlayer {
  readonly hlsUrl = input.required<string>();
  readonly poster = input<string>();
  readonly title = input<string>('Video');

  constructor() {
    // ✅ Импортируем синхронно в конструкторе
    if (typeof window !== 'undefined') {
      import('vidstack/player');
      import('vidstack/player/layouts');
      import('vidstack/player/ui');
    }

    effect(() => {
      console.log('🎬 Player URL:', this.hlsUrl());
    });
  }
}