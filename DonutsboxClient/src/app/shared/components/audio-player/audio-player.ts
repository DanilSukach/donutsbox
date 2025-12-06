import { 
  Component, 
  input,
  effect,
  AfterViewInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  signal,
  computed
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-audio-player',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audio-player.html',
  styleUrl: './audio-player.css'
})
export class AudioPlayer implements AfterViewInit, OnDestroy {
  @ViewChild('audioElement', { static: false }) audioElementRef?: ElementRef<HTMLAudioElement>;
  
  readonly src = input.required<string>();
  readonly title = input<string>('Audio');

  readonly isPlaying = signal(false);
  readonly currentTime = signal(0);
  readonly duration = signal(0);
  readonly progressPercent = signal(0);
  readonly volume = signal(1.0); // 0.0 to 1.0
  readonly isMuted = signal(false);
  private previousVolume = 1.0; // Сохраняем предыдущую громкость перед mute

  private get audioElement(): HTMLAudioElement | null {
    return this.audioElementRef?.nativeElement ?? null;
  }

  readonly volumePercent = signal(100); // 0 to 100 для отображения

  // Валидация URL - проверяем, что URL валидный перед использованием
  readonly isValidUrl = computed(() => {
    const url = this.src();
    if (!url || typeof url !== 'string') {
      return false;
    }
    
    const trimmedUrl = url.trim();
    
    // Проверка на пустые значения и базовый URL приложения
    if (trimmedUrl === '' || 
        trimmedUrl === '/' || 
        trimmedUrl === 'https://localhost:4200/' ||
        trimmedUrl === 'http://localhost:4200/' ||
        trimmedUrl.startsWith('https://localhost:4200/') && trimmedUrl.length <= 'https://localhost:4200/'.length ||
        trimmedUrl.startsWith('http://localhost:4200/') && trimmedUrl.length <= 'http://localhost:4200/'.length) {
      return false;
    }
    
    // Проверка, что URL начинается с http:// или https://
    if (!trimmedUrl.startsWith('http://') && !trimmedUrl.startsWith('https://')) {
      return false;
    }
    
    // Дополнительная проверка через URL API
    try {
      const urlObj = new URL(trimmedUrl);
      // Если это базовый URL без пути или с пустым путем, это невалидно
      const isBaseUrl = (!urlObj.pathname || urlObj.pathname === '/' || urlObj.pathname.trim() === '');
      if (isBaseUrl) {
        return false;
      }
      return true;
    } catch (e) {
      // Если не удалось распарсить URL, это невалидно
      return false;
    }
  });

  // Computed signal для валидного URL (возвращает URL только если он валидный, иначе пустую строку)
  readonly validUrl = computed(() => {
    return this.isValidUrl() ? this.src() : '';
  });

  // Вспомогательный метод для проверки невалидности URL строки
  private isInvalidUrlString(url: string): boolean {
    if (!url || typeof url !== 'string') {
      return true;
    }
    
    const trimmedUrl = url.trim();
    
    // Проверка на пустые значения и базовый URL приложения
    if (trimmedUrl === '' || 
        trimmedUrl === '/' || 
        trimmedUrl === 'https://localhost:4200/' ||
        trimmedUrl === 'http://localhost:4200/' ||
        trimmedUrl.startsWith('https://localhost:4200/') && trimmedUrl.length <= 'https://localhost:4200/'.length ||
        trimmedUrl.startsWith('http://localhost:4200/') && trimmedUrl.length <= 'http://localhost:4200/'.length) {
      return true;
    }
    
    // Проверка через URL API
    try {
      const urlObj = new URL(trimmedUrl);
      // Если это базовый URL без пути или с пустым путем, это невалидно
      const isBaseUrl = (!urlObj.pathname || urlObj.pathname === '/' || urlObj.pathname.trim() === '');
      if (isBaseUrl) {
        return true;
      }
      return false;
    } catch (e) {
      // Если не удалось распарсить URL, это невалидно
      return true;
    }
  }

  constructor() {
    effect(() => {
      const audio = this.audioElement;
      if (!audio) return;
      
      // Всегда используем validUrl для установки src
      const validUrlValue = this.validUrl();
      
      if (validUrlValue && this.isValidUrl()) {
        // Обновление URL будет обработано в ngAfterViewInit или после него
        setTimeout(() => {
          if (audio && this.isValidUrl()) {
            audio.src = this.validUrl();
            audio.load();
          }
        }, 0);
      } else {
        // Если URL невалидный или пустой, сразу очищаем src
        audio.pause();
        audio.src = '';
        audio.load();
        this.isPlaying.set(false);
        this.currentTime.set(0);
        this.progressPercent.set(0);
      }
    });
  }

  ngAfterViewInit() {
    const audio = this.audioElement;
    if (audio) {
      this.setupAudioListeners();
      // Устанавливаем src только если URL валидный
      if (this.isValidUrl()) {
        audio.src = this.validUrl();
        audio.load();
      } else {
        // Очищаем src если он невалидный
        audio.src = '';
        audio.load();
      }
      // Устанавливаем начальную громкость
      audio.volume = this.volume();
      this.volumePercent.set(this.volume() * 100);
    }
  }

  ngOnDestroy() {
    const audio = this.audioElement;
    if (audio) {
      audio.pause();
      audio.src = '';
      audio.load(); // Очищаем загрузку
      // Удаляем все слушатели событий
      audio.removeEventListener('loadedmetadata', () => {});
      audio.removeEventListener('timeupdate', () => {});
      audio.removeEventListener('play', () => {});
      audio.removeEventListener('pause', () => {});
      audio.removeEventListener('ended', () => {});
      audio.removeEventListener('error', () => {});
    }
  }

  private setupAudioListeners() {
    const audio = this.audioElement;
    if (!audio) return;

    // Устанавливаем withCredentials для поддержки куков
    if ('withCredentials' in audio) {
      (audio as any).withCredentials = true;
    }

    audio.addEventListener('loadedmetadata', () => {
      this.duration.set(audio.duration || 0);
    });

    audio.addEventListener('timeupdate', () => {
      if (audio.duration > 0) {
        this.currentTime.set(audio.currentTime);
        const percent = (audio.currentTime / audio.duration) * 100;
        this.progressPercent.set(percent);
      }
    });

    audio.addEventListener('play', () => {
      this.isPlaying.set(true);
    });

    audio.addEventListener('pause', () => {
      this.isPlaying.set(false);
    });

    audio.addEventListener('ended', () => {
      this.isPlaying.set(false);
      this.currentTime.set(0);
      this.progressPercent.set(0);
    });

    audio.addEventListener('error', (e) => {
      const audioEl = e.target as HTMLAudioElement;
      if (!audioEl) return;
      
      const currentSrc = audioEl.src || '';
      
      // Проверяем, является ли текущий src невалидным URL
      const isInvalidUrl = this.isInvalidUrlString(currentSrc);
      
      if (isInvalidUrl) {
        // Если URL невалидный, просто очищаем src и не логируем ошибку
        audioEl.src = '';
        audioEl.load();
        return;
      }
      
      // Проверяем также через isValidUrl для текущего src()
      if (!this.isValidUrl()) {
        audioEl.src = '';
        audioEl.load();
        return;
      }
      
      // Дополнительная проверка: если URL является базовым (без пути), не логируем ошибку
      try {
        const urlObj = new URL(currentSrc);
        if (!urlObj.pathname || urlObj.pathname === '/' || urlObj.pathname.trim() === '') {
          // Базовый URL без пути - не логируем ошибку
          audioEl.src = '';
          audioEl.load();
          return;
        }
      } catch (e) {
        // Невалидный URL - не логируем ошибку
        audioEl.src = '';
        audioEl.load();
        return;
      }
      
      const error = audioEl.error;
      if (error) {
        // Логируем только если URL валидный и не является базовым
        let errorMessage = 'Unknown error';
        switch (error.code) {
          case error.MEDIA_ERR_ABORTED:
            errorMessage = 'Audio loading aborted';
            break;
          case error.MEDIA_ERR_NETWORK:
            errorMessage = 'Network error while loading audio';
            break;
          case error.MEDIA_ERR_DECODE:
            errorMessage = 'Audio decoding error';
            break;
          case error.MEDIA_ERR_SRC_NOT_SUPPORTED:
            errorMessage = 'Audio format not supported or source not found';
            break;
        }
        console.error('Audio error:', errorMessage, 'Code:', error.code, 'URL:', audioEl.src);
      }
    });
  }

  togglePlayPause() {
    const audio = this.audioElement;
    if (!audio) return;

    if (audio.paused) {
      audio.play().catch(err => {
        console.error('Error playing audio:', err);
      });
    } else {
      audio.pause();
    }
  }

  seek(event: MouseEvent) {
    const audio = this.audioElement;
    if (!audio || !audio.duration) return;

    const progressContainer = event.currentTarget as HTMLElement;
    const rect = progressContainer.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const percent = Math.max(0, Math.min(100, (clickX / rect.width) * 100));
    const newTime = (percent / 100) * audio.duration;

    audio.currentTime = newTime;
    this.currentTime.set(newTime);
    this.progressPercent.set(percent);
  }

  formatTime(seconds: number): string {
    if (!isFinite(seconds) || isNaN(seconds) || seconds < 0) return '0:00';
    
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  onPlay() {
    this.isPlaying.set(true);
  }

  onPause() {
    this.isPlaying.set(false);
  }

  onTimeUpdate() {
    const audio = this.audioElement;
    if (audio && audio.duration > 0) {
      this.currentTime.set(audio.currentTime);
      this.progressPercent.set((audio.currentTime / audio.duration) * 100);
    }
  }

  onLoadedMetadata() {
    const audio = this.audioElement;
    if (audio) {
      this.duration.set(audio.duration || 0);
    }
  }

  onEnded() {
    this.isPlaying.set(false);
    this.currentTime.set(0);
    this.progressPercent.set(0);
  }

  toggleMute() {
    const audio = this.audioElement;
    if (!audio) return;

    if (this.isMuted()) {
      // Unmute: восстанавливаем предыдущую громкость
      this.isMuted.set(false);
      this.volume.set(this.previousVolume);
      audio.volume = this.previousVolume;
      audio.muted = false;
    } else {
      // Mute: сохраняем текущую громкость и устанавливаем 0
      this.previousVolume = this.volume();
      this.isMuted.set(true);
      audio.muted = true;
    }
  }

  setVolume(event: MouseEvent) {
    const audio = this.audioElement;
    if (!audio) return;

    const volumeContainer = event.currentTarget as HTMLElement;
    const rect = volumeContainer.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const percent = Math.max(0, Math.min(100, (clickX / rect.width) * 100));
    const newVolume = percent / 100;

    this.volume.set(newVolume);
    this.volumePercent.set(percent);
    audio.volume = newVolume;
    audio.muted = false;
    this.isMuted.set(false);
    
    // Сохраняем громкость для unmute
    this.previousVolume = newVolume;
  }
}

