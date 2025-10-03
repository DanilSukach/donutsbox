import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { newCreatorGuard } from './new-creator-guard';

describe('newCreatorGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => newCreatorGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
