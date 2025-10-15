import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreatePostModal } from './create-post-modal';

describe('CreatePostModal', () => {
  let component: CreatePostModal;
  let fixture: ComponentFixture<CreatePostModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreatePostModal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreatePostModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
