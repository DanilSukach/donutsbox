import { Component, output, signal, input, OnDestroy } from '@angular/core';

@Component({
  selector: 'app-audio-recorder',
  standalone: true,
  imports: [],
  templateUrl: './audio-recorder.html',
  styleUrl: './audio-recorder.css'
})
export class AudioRecorder implements OnDestroy {
  readonly recorded = output<Blob>();
  readonly cancelled = output<void>();
  
  // Input для проверки названия перед записью
  readonly audioTitle = input<string>('');

  readonly isRecording = signal(false);
  readonly isPaused = signal(false);
  readonly recordingTime = signal(0);
  readonly hasRecording = signal(false);
  readonly error = signal<string | null>(null);

  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];
  private stream: MediaStream | null = null;
  private timerInterval: number | null = null;
  private startTime: number = 0;

  async startRecording(): Promise<void> {
    // Проверяем, что название аудио введено
    if (!this.audioTitle() || this.audioTitle().trim() === '') {
      this.error.set('Пожалуйста, введите название аудио перед началом записи');
      return;
    }
    
    try {
      this.error.set(null);
      
      // Запрашиваем доступ к микрофону
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      
      // Создаем MediaRecorder
      const options: MediaRecorderOptions = {
        mimeType: this.getSupportedMimeType()
      };
      
      this.mediaRecorder = new MediaRecorder(this.stream, options);
      this.audioChunks = [];

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: this.mediaRecorder?.mimeType || 'audio/webm' });
        this.hasRecording.set(true);
        this.stopStream();
      };

      this.mediaRecorder.onerror = (event) => {
        this.error.set('Ошибка записи аудио');
        console.error('MediaRecorder error:', event);
      };

      this.mediaRecorder.start();
      this.isRecording.set(true);
      this.isPaused.set(false);
      this.startTime = Date.now();
      this.startTimer();
    } catch (err) {
      this.error.set('Не удалось получить доступ к микрофону. Проверьте разрешения браузера.');
      console.error('Error accessing microphone:', err);
    }
  }

  pauseRecording(): void {
    if (this.mediaRecorder && this.isRecording() && !this.isPaused()) {
      this.mediaRecorder.pause();
      this.isPaused.set(true);
      this.stopTimer();
    }
  }

  resumeRecording(): void {
    if (this.mediaRecorder && this.isPaused()) {
      this.mediaRecorder.resume();
      this.isPaused.set(false);
      this.startTimer();
    }
  }

  stopRecording(): void {
    if (this.mediaRecorder && this.isRecording()) {
      this.mediaRecorder.stop();
      this.isRecording.set(false);
      this.isPaused.set(false);
      this.stopTimer();
    }
  }

  saveRecording(): void {
    if (this.audioChunks.length > 0) {
      const audioBlob = new Blob(this.audioChunks, { 
        type: this.mediaRecorder?.mimeType || 'audio/webm' 
      });
      this.recorded.emit(audioBlob);
      this.reset();
    }
  }

  cancelRecording(): void {
    this.reset();
    this.cancelled.emit();
  }

  private reset(): void {
    this.stopRecording();
    this.stopStream();
    this.audioChunks = [];
    this.isRecording.set(false);
    this.isPaused.set(false);
    this.hasRecording.set(false);
    this.recordingTime.set(0);
    this.error.set(null);
  }

  private stopStream(): void {
    if (this.stream) {
      this.stream.getTracks().forEach(track => track.stop());
      this.stream = null;
    }
  }

  private startTimer(): void {
    this.stopTimer();
    this.timerInterval = window.setInterval(() => {
      if (this.isRecording() && !this.isPaused()) {
        const elapsed = Math.floor((Date.now() - this.startTime) / 1000);
        this.recordingTime.set(elapsed);
      }
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timerInterval !== null) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  private getSupportedMimeType(): string {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/ogg;codecs=opus',
      'audio/mp4',
      'audio/wav'
    ];

    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        return type;
      }
    }

    return ''; // Браузер выберет сам
  }

  formatTime(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  ngOnDestroy(): void {
    this.reset();
  }
}

