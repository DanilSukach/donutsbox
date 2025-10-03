import { TestBed } from '@angular/core/testing';

import { ProfileFacade } from './profile-facade';

describe('ProfileFacade', () => {
  let service: ProfileFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProfileFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
