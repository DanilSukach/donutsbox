import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { creatorGuard } from './creator-guard';

describe('creatorGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => creatorGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
