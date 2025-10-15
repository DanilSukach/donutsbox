import { inject, Injectable } from '@angular/core';
import { AddVideosRequestDto, AddVideosResponseDto, CreateDraftRequestDto, CreatorPostService, CreatorPostsResponseDto, FilesService, MessageResponseDto, MyPostsResponseDto, MyVideoResponseDto, PostDraftResponseDto, PublishPostResponseDto, UploadImagesResponseDto, VideoUploadResponseDto } from '@app/api/donutsbox';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PostsFacade {
  private creatorPostService = inject(CreatorPostService);
  private filesService = inject(FilesService);
  
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
    return this.creatorPostService.apiCreatorPostPostIdUnpublishPost(postId);
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
  return `/api/creator/posts/images/${imagePath}`;
  }
}
