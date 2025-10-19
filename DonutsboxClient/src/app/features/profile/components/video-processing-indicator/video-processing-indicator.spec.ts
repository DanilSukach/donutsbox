import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoProcessingIndicator } from './video-processing-indicator';

describe('VideoProcessingIndicator', () => {
  let component: VideoProcessingIndicator;
  let fixture: ComponentFixture<VideoProcessingIndicator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoProcessingIndicator]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VideoProcessingIndicator);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

