import { TestBed } from '@angular/core/testing';

import { CommentsFacade } from './comments-facade';

describe('CommentsFacade', () => {
  let service: CommentsFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CommentsFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
