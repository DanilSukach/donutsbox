import { TestBed } from '@angular/core/testing';

import { PostsFacade } from './posts-facade';

describe('PostsFacade', () => {
  let service: PostsFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PostsFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
