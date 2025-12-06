// DonutsboxClient/src/app/shared/components/video-player/video-player.ts
import { 
  Component, 
  input,
  CUSTOM_ELEMENTS_SCHEMA,
  effect,
  AfterViewInit,
  ElementRef,
  ViewChild
} from '@angular/core';

@Component({
  selector: 'app-video-player',
  standalone: true,
  imports: [],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './video-player.html',
  styleUrl: './video-player.css'
})
export class VideoPlayer implements AfterViewInit {
  @ViewChild('mediaPlayer', { static: false }) mediaPlayerRef?: ElementRef<HTMLElement>;
  
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
      const url = this.hlsUrl();
      const poster = this.poster();
      console.log('🎬 VideoPlayer effect triggered');
      console.log('🎬 Player URL:', url);
      console.log('🖼️ Poster URL:', poster);
      
      // Проверяем, что URL валидный
      if (!url || url.trim() === '') {
        console.error('❌ VideoPlayer: hlsUrl is empty or invalid!');
      } else if (!url.includes('.m3u8')) {
        console.warn('⚠️ VideoPlayer: hlsUrl does not contain .m3u8:', url);
      }
    });
  }

  ngAfterViewInit() {
    if (typeof window === 'undefined') {
      console.warn('⚠️ VideoPlayer: window is undefined (SSR)');
      return;
    }
    
    if (!this.mediaPlayerRef) {
      console.error('❌ VideoPlayer: mediaPlayerRef is null');
      return;
    }

    // Настраиваем HLS.js для предзагрузки сегментов
    setTimeout(() => {
      const player = this.mediaPlayerRef?.nativeElement;
      if (!player) {
        console.error('❌ VideoPlayer: player element not found');
        return;
      }
      

      // Получаем доступ к HLS инстансу через vidstack
      const setupHLS = () => {
        // @ts-ignore - доступ к внутреннему API vidstack
        const provider = player?.provider;
        if (provider && provider.type === 'hls') {
          // @ts-ignore - доступ к HLS инстансу
          const hls = provider.instance;
          
          if (hls && hls.config) {
            // Настройка предзагрузки сегментов
            hls.config.maxBufferLength = 30; // Максимальная длина буфера в секундах
            hls.config.maxMaxBufferLength = 60; // Максимальная максимальная длина буфера
            hls.config.maxBufferSize = 60 * 1000 * 1000; // 60MB буфер
            hls.config.maxBufferHole = 0.5; // Максимальная дыра в буфере
            hls.config.highBufferWatchdogPeriod = 2; // Период проверки буфера
            hls.config.nudgeOffset = 0.1; // Смещение для nudging
            hls.config.nudgeMaxRetry = 3; // Максимальное количество попыток nudging
            hls.config.maxFragLoadingTimeOut = 20; // Таймаут загрузки фрагмента
            hls.config.fragLoadingTimeOut = 20; // Таймаут загрузки фрагмента
            hls.config.manifestLoadingTimeOut = 10; // Таймаут загрузки манифеста
            
            // Предзагрузка следующего сегмента
            hls.config.backBufferLength = 30; // Длина обратного буфера

            // Обработка ошибок HLS (проверяем наличие Events)
            if (hls.Events && typeof hls.on === 'function') {
              try {
                hls.on(hls.Events.ERROR, (event: any, data: any) => {
                  if (data && data.fatal) {
                    console.error('HLS fatal error:', data);
                  }
                });
              } catch (error) {
                console.warn('Failed to set up HLS error handler:', error);
              }
            }
          }
        }
      };

      // Пытаемся настроить HLS сразу
      setupHLS();

      // Также слушаем событие provider-setup
      player.addEventListener('provider-setup', () => {
        setTimeout(setupHLS, 100);
      });
    }, 200);
  }
}