import { inject, Injectable } from '@angular/core';
import { CreatePostCommentDto, PostCommentDto, PostCommentService, UpdateCommentRequestDto } from '@app/api/donutsbox';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root'
})
export class CommentsFacade {
  private postCommentService = inject(PostCommentService);
  private hubConnection?: signalR.HubConnection;
  private joinedPosts = new Set<string>();

  public commentAdded$ = new Subject<PostCommentDto>();
  public commentUpdated$ = new Subject<{ id: string; text: string }>();
  public commentDeleted$ = new Subject<string>();
  public connectionState$ = new Subject<signalR.HubConnectionState>();

  constructor() { 
    this.initSignalR(); 
  }

  private initSignalR(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.commentsHubUrl, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Debug)
      .build();

    this.hubConnection.on('CommentAdded', (comment: PostCommentDto) => {
      this.commentAdded$.next(comment);
    });

    this.hubConnection.on('CommentUpdated', (data: { id: string; text: string }) => {
      this.commentUpdated$.next(data);
    });

    this.hubConnection.on('CommentDeleted', (commentId: string) => {
      this.commentDeleted$.next(commentId);
    });

    this.hubConnection.start()
      .then(() => {
        this.connectionState$.next(signalR.HubConnectionState.Connected);
        this.rejoinJoinedPosts();
      })
      .catch(err => {
        this.connectionState$.next(signalR.HubConnectionState.Disconnected);
      });

    this.hubConnection.onreconnecting(() => {
      this.connectionState$.next(signalR.HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState$.next(signalR.HubConnectionState.Connected);
      this.rejoinJoinedPosts();
    });

    this.hubConnection.onclose(() => {
      this.connectionState$.next(signalR.HubConnectionState.Disconnected);
    });
  }

  private rejoinJoinedPosts(): void {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) return;
    
    this.joinedPosts.forEach(postId => {
      this.hubConnection!.invoke('JoinPostComments', postId)
        .then(() => console.log(`✅ [SignalR] Rejoined post: ${postId}`))
        .catch(err => console.error(`❌ [SignalR] Failed to rejoin post ${postId}:`, err));
    });
  }

  joinPostComments(postId: string): void {
    this.joinedPosts.add(postId);
    
    if (!this.hubConnection) {
      return;
    }

    if (this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection.invoke('JoinPostComments', postId)
      .then(() => console.log(`✅ [SignalR] Joined group: post-${postId}`))
      .catch(err => console.error(`❌ [SignalR] Failed to join group: post-${postId}`, err));
  }

  leavePostComments(postId: string): void {
    console.log(`🚪 Leaving post comments group: ${postId}`);
    this.joinedPosts.delete(postId);
    
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('LeavePostComments', postId)
        .then(() => console.log(`✅ [SignalR] Left group: post-${postId}`))
        .catch(err => console.error(`❌ [SignalR] Failed to leave group: post-${postId}`, err));
    }
  }

  getPostComments(postId: string): Observable<Array<PostCommentDto>> {
    return this.postCommentService.apiPostCommentPostPostIdGet(postId);
  }

  addComment(postId: string, text: string): Observable<PostCommentDto> {
    const dto: CreatePostCommentDto = { postId, text };
    return this.postCommentService.apiPostCommentPost(dto);
  }

  updateComment(commentId: string, text: string): Observable<any> {
    const dto: UpdateCommentRequestDto = { text };
    return this.postCommentService.apiPostCommentIdPut(commentId, dto);
  }

  deleteComment(commentId: string): Observable<any> {
    return this.postCommentService.apiPostCommentIdDelete(commentId);
  }
}
