import { inject, Injectable } from '@angular/core';
import { AddTextRequestDto, AddTextResponseDto, AddVideosRequestDto, AddVideosResponseDto, ContentPostReactionDto, CreateDraftRequestDto, CreatorPostService, CreatorPostsResponseDto, FilesService, MessageResponseDto, MyPostsResponseDto, MyVideoResponseDto, PostDraftResponseDto, PublishPostResponseDto, SubscriptionDto, UploadImagesResponseDto, UserInteractionService, VideoUploadResponseDto } from '@app/api/donutsbox';
import { catchError, Observable, tap, throwError } from 'rxjs';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class PostsFacade {
  private creatorPostService = inject(CreatorPostService);
  private postsRefresh = inject(PostsRefresh)
  private filesService = inject(FilesService);
  private userInteractionService = inject(UserInteractionService);
  private http = inject(HttpClient);
  
  createDraft(request: CreateDraftRequestDto): Observable<PostDraftResponseDto> {
    return this.creatorPostService.apiCreatorPostDraftPost(request);
  }

  uploadVideo(
    contentPostId: string,
    title: string,
    file: Blob,
    description?: string,
    thumbnail?: Blob
  ): Observable<VideoUploadResponseDto> {
    return this.filesService.apiFilesUploadPost(
      contentPostId,
      title,
      description,
      file,
      thumbnail
    );
  }

  addVideosToPost(postId: string, request: AddVideosRequestDto): Observable<AddVideosResponseDto> {
    return this.creatorPostService.apiCreatorPostPostIdVideosPost(postId, request);
  }

  publishPost(postId: string): Observable<PublishPostResponseDto> {
    return this.creatorPostService.apiCreatorPostPostIdPublishPost(postId);
  }

  unpublishPost(postId: string): Observable<MessageResponseDto> {
    return this.creatorPostService.apiCreatorPostPostIdUnpublishPost(postId).pipe(
      catchError((error) => {
        console.error('Ошибка снятия публикации поста:', error);
        return throwError(() => error);
      })
    );
  }

  getMyPosts(
    page: number = 1,
    pageSize: number = 20,
    isPublished?: boolean
  ): Observable<MyPostsResponseDto> {
    return this.creatorPostService.apiCreatorPostMyGet(page, pageSize, isPublished);
  }

  getMyVideos(
    page: number = 1,
    pageSize: number = 20,
    status?: string
  ): Observable<MyVideoResponseDto> {
    return this.filesService.apiFilesMyVideosGet(page, pageSize, status);
  }

  getCreatorPosts(
    creatorId: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<CreatorPostsResponseDto> {
    return this.creatorPostService.apiCreatorPostCreatorCreatorIdGet(creatorId, page, pageSize);
  }

  uploadImages(images: Array<Blob>): Observable<UploadImagesResponseDto> {
    return this.creatorPostService.apiCreatorPostUploadImagesPost(images);
  }

  getVideoHlsUrl(videoId: string): string {
    return `/api/files/${videoId}/hls/index.m3u8`;
  }

  getVideoThumbnailUrl(videoId: string): string {
    return `/api/files/${videoId}/thumbnail`;
  }

  getPostImageUrl(imagePath: string): string {
    // Если URL уже полный (presigned URL), возвращаем его как есть
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    // Иначе добавляем префикс для старых относительных путей
    return `/api/creator/posts/images/${imagePath}`;
  }

  getCreatorSubscriptions(): Observable<SubscriptionDto[]> {
    return this.http.get<SubscriptionDto[]>('/api/creator-subscriptions/my');
  }

  deletePost(postId: string): Observable<any> {
    return this.creatorPostService.apiCreatorPostPostIdDelete(postId).pipe(
      tap(() => {
        console.log('Пост удален, обновляем список');
        this.postsRefresh.triggerRefresh();
      }),
      catchError((error) => {
        console.error('Ошибка удаления поста:', error);
        return throwError(() => error);
      })
    );
  }

  changeReaction(postId: string, reactionTypeId: number): Observable<any> {
    const reaction: ContentPostReactionDto = {
      postId: postId,
      reactionTypeId: reactionTypeId
    };
    
    return this.userInteractionService.apiUserInteractionChangeReactionPost(reaction).pipe(
      catchError((error) => {
        console.error('Ошибка изменения реакции:', error);
        return throwError(() => error);
      })
    );
  }

  updatePostText(postId: string, title: string, text: string): Observable<AddTextResponseDto> {
    const request: AddTextRequestDto = {
      title: title,
      text: text
    };
    
    // Используем PUT запрос напрямую, так как API клиент еще не обновлен
    return this.http.put<AddTextResponseDto>(`/api/CreatorPost/${postId}/text`, request).pipe(
      tap(() => {
        console.log('Пост обновлен успешно');
        // Не обновляем список постов, чтобы избежать перезагрузки страницы
      }),
      catchError((error) => {
        console.error('Ошибка обновления поста:', error);
        return throwError(() => error);
      })
    );
  }
}
