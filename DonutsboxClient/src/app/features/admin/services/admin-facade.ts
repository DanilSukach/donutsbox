import { inject, Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { 
  AdminUserService, 
  AdminContentService,
  AdminAuthorListDto,
  AdminContentPostListDto,
  AdminActionResponseDto,
  AdminUserListDto
} from '@app/api/admin';

// Export types for backward compatibility
export type AdminUser = AdminUserListDto;
export type AdminAuthor = AdminAuthorListDto;
export type AdminContentPost = AdminContentPostListDto;

// Re-export types from generated API
export type { AdminUserListDto, AdminAuthorListDto, AdminContentPostListDto, AdminActionResponseDto } from '@app/api/admin';

// Facade using generated API services
@Injectable({
  providedIn: 'root'
})
export class AdminFacade {
  private adminUserService = inject(AdminUserService);
  private adminContentService = inject(AdminContentService);

  getAllAuthors(): Observable<AdminAuthorListDto[]> {
    return this.adminUserService.apiAdminAdminUserAuthorsGet().pipe(
      catchError((err) => {
        console.error('Error loading authors:', err);
        return of([]);
      })
    );
  }

  getAllPosts(): Observable<AdminContentPostListDto[]> {
    return this.adminContentService.apiAdminAdminContentPostsGet().pipe(
      catchError((err) => {
        console.error('Error loading posts:', err);
        return of([]);
      })
    );
  }

  shadowBanPost(postId: string): Observable<AdminActionResponseDto> {
    return this.adminContentService.apiAdminAdminContentPostsPostIdShadowbanPost(postId).pipe(
      catchError((err) => {
        console.error('Error shadow banning post:', err);
        return of({ success: false, message: err.error?.message || 'Ошибка теневого бана поста' });
      })
    );
  }

  unshadowBanPost(postId: string): Observable<AdminActionResponseDto> {
    return this.adminContentService.apiAdminAdminContentPostsPostIdUnshadowbanPost(postId).pipe(
      catchError((err) => {
        console.error('Error unshadow banning post:', err);
        return of({ success: false, message: err.error?.message || 'Ошибка снятия теневого бана с поста' });
      })
    );
  }

  shadowBanAuthor(creatorPageId: string): Observable<AdminActionResponseDto> {
    return this.adminUserService.apiAdminAdminUserAuthorsCreatorPageIdShadowbanPost(creatorPageId).pipe(
      catchError((err) => {
        console.error('Error shadow banning author:', err);
        return of({ success: false, message: err.error?.message || 'Ошибка теневого бана автора' });
      })
    );
  }

  unshadowBanAuthor(creatorPageId: string): Observable<AdminActionResponseDto> {
    return this.adminUserService.apiAdminAdminUserAuthorsCreatorPageIdUnshadowbanPost(creatorPageId).pipe(
      catchError((err) => {
        console.error('Error unshadow banning author:', err);
        return of({ success: false, message: err.error?.message || 'Ошибка снятия теневого бана с автора' });
      })
    );
  }
}
