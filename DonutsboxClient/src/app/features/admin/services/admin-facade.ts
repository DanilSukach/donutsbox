import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpContext } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs matching backend structure
export interface AdminUserListDto {
  id?: string;
  name?: string;
  email?: string;
  userType?: string;
  createdAt?: string;
  hasCreatorPage?: boolean;
  postsCount?: number;
  subscriptionsCount?: number;
}

export interface AdminAuthorListDto {
  id?: string;
  creatorPageId?: string;
  name?: string;
  email?: string;
  userType?: string;
  createdAt?: string;
  postsCount?: number;
  subscriptionsCount?: number;
  subscribersCount?: number;
  isShadowBanned?: boolean;
}

export interface AdminContentPostListDto {
  id?: string;
  title?: string;
  text?: string;
  creatorPageDataId?: string;
  creatorName?: string;
  isPublished?: boolean;
  isShadowBanned?: boolean;
  createdAt?: string;
  likesCount?: number;
  dislikesCount?: number;
  commentsCount?: number;
  mediaCount?: number;
}

export interface AdminActionResponseDto {
  success?: boolean;
  message?: string;
}

// Service class following generated API pattern
@Injectable({
  providedIn: 'root'
})
class AdminUserService {
  protected basePath = environment.adminApiBaseUrl || environment.donutsboxApiBaseUrl;
  protected defaultHeaders = new HttpHeaders();

  constructor(protected httpClient: HttpClient) {}

  /**
   * Получить список всех авторов
   */
  public apiAdminAdminUserAuthorsGet(observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<Array<AdminAuthorListDto>>;
  public apiAdminAdminUserAuthorsGet(observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminUserAuthorsGet(observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.get<Array<AdminAuthorListDto>>(`${this.basePath}/adminuser/authors`, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }

  /**
   * Добавить автора в теневой бан
   */
  public apiAdminAdminUserAuthorsCreatorPageIdShadowbanPost(creatorPageId: string, observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<AdminActionResponseDto>;
  public apiAdminAdminUserAuthorsCreatorPageIdShadowbanPost(creatorPageId: string, observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminUserAuthorsCreatorPageIdShadowbanPost(creatorPageId: string, observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    if (creatorPageId === null || creatorPageId === undefined) {
      throw new Error('Required parameter creatorPageId was null or undefined when calling apiAdminAdminUserAuthorsCreatorPageIdShadowbanPost.');
    }
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.post<AdminActionResponseDto>(`${this.basePath}/adminuser/authors/${encodeURIComponent(String(creatorPageId))}/shadowban`, null, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }

  /**
   * Снять теневой бан с автора
   */
  public apiAdminAdminUserAuthorsCreatorPageIdUnshadowbanPost(creatorPageId: string, observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<AdminActionResponseDto>;
  public apiAdminAdminUserAuthorsCreatorPageIdUnshadowbanPost(creatorPageId: string, observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminUserAuthorsCreatorPageIdUnshadowbanPost(creatorPageId: string, observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    if (creatorPageId === null || creatorPageId === undefined) {
      throw new Error('Required parameter creatorPageId was null or undefined when calling apiAdminAdminUserAuthorsCreatorPageIdUnshadowbanPost.');
    }
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.post<AdminActionResponseDto>(`${this.basePath}/adminuser/authors/${encodeURIComponent(String(creatorPageId))}/unshadowban`, null, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }
}

@Injectable({
  providedIn: 'root'
})
class AdminContentService {
  protected basePath = environment.adminApiBaseUrl || environment.donutsboxApiBaseUrl;
  protected defaultHeaders = new HttpHeaders();

  constructor(protected httpClient: HttpClient) {}

  /**
   * Получить список всех постов
   */
  public apiAdminAdminContentPostsGet(observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<Array<AdminContentPostListDto>>;
  public apiAdminAdminContentPostsGet(observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminContentPostsGet(observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.get<Array<AdminContentPostListDto>>(`${this.basePath}/admincontent/posts`, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }

  /**
   * Добавить пост в теневой бан
   */
  public apiAdminAdminContentPostsPostIdShadowbanPost(postId: string, observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<AdminActionResponseDto>;
  public apiAdminAdminContentPostsPostIdShadowbanPost(postId: string, observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminContentPostsPostIdShadowbanPost(postId: string, observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    if (postId === null || postId === undefined) {
      throw new Error('Required parameter postId was null or undefined when calling apiAdminAdminContentPostsPostIdShadowbanPost.');
    }
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.post<AdminActionResponseDto>(`${this.basePath}/admincontent/posts/${encodeURIComponent(String(postId))}/shadowban`, null, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }

  /**
   * Снять теневой бан с поста
   */
  public apiAdminAdminContentPostsPostIdUnshadowbanPost(postId: string, observe?: 'body', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<AdminActionResponseDto>;
  public apiAdminAdminContentPostsPostIdUnshadowbanPost(postId: string, observe?: 'response', reportProgress?: boolean, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any>;
  public apiAdminAdminContentPostsPostIdUnshadowbanPost(postId: string, observe: any = 'body', reportProgress: boolean = false, options?: {httpHeaderAccept?: 'application/json', context?: HttpContext}): Observable<any> {
    if (postId === null || postId === undefined) {
      throw new Error('Required parameter postId was null or undefined when calling apiAdminAdminContentPostsPostIdUnshadowbanPost.');
    }
    let localVarHeaders = this.defaultHeaders;
    const localVarHttpHeaderAcceptSelected: string | undefined = options?.httpHeaderAccept ?? 'application/json';
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }
    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();

    return this.httpClient.post<AdminActionResponseDto>(`${this.basePath}/admincontent/posts/${encodeURIComponent(String(postId))}/unshadowban`, null, {
      context: localVarHttpContext,
      headers: localVarHeaders,
      observe: observe,
      reportProgress: reportProgress,
      withCredentials: true
    });
  }
}

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

// Export types for backward compatibility
export type AdminUser = AdminUserListDto;
export type AdminAuthor = AdminAuthorListDto;
export type AdminContentPost = AdminContentPostListDto;
