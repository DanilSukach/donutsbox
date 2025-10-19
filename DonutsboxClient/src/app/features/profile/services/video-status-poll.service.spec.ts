import { TestBed } from '@angular/core/testing';

import { VideoStatusPollService } from './video-status-poll.service';

describe('VideoStatusPollService', () => {
  let service: VideoStatusPollService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(VideoStatusPollService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

