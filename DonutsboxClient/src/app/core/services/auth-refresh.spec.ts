import { TestBed } from '@angular/core/testing';

import { AuthRefresh } from './auth-refresh';

describe('AuthRefresh', () => {
  let service: AuthRefresh;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthRefresh);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
