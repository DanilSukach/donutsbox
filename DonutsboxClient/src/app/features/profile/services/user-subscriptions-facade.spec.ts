import { TestBed } from '@angular/core/testing';

import { UserSubscriptionsFacade } from './user-subscriptions-facade';

describe('UserSubscriptionsFacade', () => {
  let service: UserSubscriptionsFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(UserSubscriptionsFacade);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
