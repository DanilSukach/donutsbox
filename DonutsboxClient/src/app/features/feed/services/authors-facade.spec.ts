import { TestBed } from '@angular/core/testing';
import { AuthorsFacade } from './authors-facade';

describe('AuthorsFacade', () => {
  let service: AuthorsFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthorsFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have initial state', () => {
    expect(service.authors()).toEqual([]);
    expect(service.isLoadingAuthors()).toBeFalsy();
    expect(service.selectedAuthor()).toBeNull();
  });
});
