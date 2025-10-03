import { TestBed } from '@angular/core/testing';
import { FeedFacade } from './feed-facade';

describe('FeedFacade', () => {
  let service: FeedFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FeedFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize user data on creation', () => {
    expect(service.userGuid).toBeDefined();
    expect(service.topAuthors).toBeDefined();
    expect(service.isLoadingTopAuthors).toBeDefined();
  });
});