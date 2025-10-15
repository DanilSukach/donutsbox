import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PostsList } from './posts-list';

describe('PostsList', () => {
  let component: PostsList;
  let fixture: ComponentFixture<PostsList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostsList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PostsList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
