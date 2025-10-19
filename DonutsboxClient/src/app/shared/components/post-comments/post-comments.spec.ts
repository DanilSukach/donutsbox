import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PostComments } from './post-comments';

describe('PostComments', () => {
  let component: PostComments;
  let fixture: ComponentFixture<PostComments>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostComments]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PostComments);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
