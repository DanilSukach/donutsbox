import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserSubscriptions } from './user-subscriptions';

describe('UserSubscriptions', () => {
  let component: UserSubscriptions;
  let fixture: ComponentFixture<UserSubscriptions>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserSubscriptions]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserSubscriptions);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
