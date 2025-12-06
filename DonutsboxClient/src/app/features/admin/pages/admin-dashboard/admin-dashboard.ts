import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminFacade, AdminAuthor, AdminContentPost } from '../../services/admin-facade';
import { AuthFacade } from '@app/features/auth/services/auth-facade';
import { SessionService } from '@app/core/services/session.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-dashboard">
      <div class="dashboard-header">
        <h1>Админ панель</h1>
        <button (click)="logout()" class="logout-btn">Выйти</button>
      </div>
      
      <div class="search-section">
        <input 
          type="text" 
          [value]="searchQuery()" 
          (input)="onSearchInput($any($event.target).value)"
          [placeholder]="activeTab() === 'posts' ? 'Поиск постов...' : 'Поиск авторов...'"
          class="search-input"
        />
        <button (click)="loadAllData()">Обновить</button>
      </div>

      <div *ngIf="loading()" class="loading">Загрузка...</div>
      <div *ngIf="error()" class="error">{{ error() }}</div>

      <div class="content-wrapper" *ngIf="!loading()">
        <!-- Левая панель: Список пользователей/авторов -->
        <div class="users-panel">
          <div class="tabs">
            <button 
              [class.active]="activeTab() === 'authors'" 
              (click)="setTab('authors')"
            >
              Авторы ({{ filteredAuthors().length }})
            </button>
            <button 
              [class.active]="activeTab() === 'posts'" 
              (click)="setTab('posts')"
            >
              Посты ({{ filteredPosts().length }})
            </button>
          </div>

          <div class="users-list">
            <!-- Authors List -->
            <ng-container *ngIf="activeTab() === 'authors'">
                <div 
                  *ngFor="let author of filteredAuthors()" 
                  class="user-item"
                  [class.selected]="selectedAuthorId() === author.creatorPageId"
                  (click)="selectAuthor(author.creatorPageId || undefined)"
                >
                <div class="user-info">
                  <strong>{{ author.name || 'Без имени' }}</strong>
                  <span class="email">{{ author.email || 'Нет email' }}</span>
                  <div class="author-stats">
                    <span>Подписчики: {{ author.subscribersCount || 0 }}</span>
                    <span>Посты: {{ author.postsCount || 0 }}</span>
                  </div>
                  <span *ngIf="author.isShadowBanned" class="shadow-banned-badge">Теневой бан</span>
                </div>
              </div>
            </ng-container>

            <!-- Posts List -->
            <ng-container *ngIf="activeTab() === 'posts'">
              <div 
                *ngFor="let post of filteredPosts()" 
                class="user-item post-item-list"
                [class.selected]="selectedPostId() === post.id"
                (click)="selectPost(post.id || undefined)"
              >
                <div class="user-info">
                  <strong>{{ post.title || 'Без заголовка' }}</strong>
                  <span class="email">Автор: {{ post.creatorName || 'Неизвестно' }}</span>
                  <div class="post-meta-list">
                    <span [class.published]="post.isPublished" [class.unpublished]="!post.isPublished">
                      {{ post.isPublished ? 'Опубликован' : 'Не опубликован' }}
                    </span>
                    <span *ngIf="post.isShadowBanned" class="shadow-banned-badge">Теневой бан</span>
                  </div>
                  <div class="post-stats-list">
                    <span>Лайки: {{ post.likesCount || 0 }}</span>
                    <span>Комментарии: {{ post.commentsCount || 0 }}</span>
                  </div>
                </div>
              </div>
            </ng-container>

            <div *ngIf="filteredAuthors().length === 0 && filteredPosts().length === 0" class="empty">
              {{ searchQuery() ? 'Ничего не найдено' : 'Нет данных' }}
            </div>
          </div>
        </div>

        <!-- Правая панель: Контент выбранного пользователя/автора/поста -->
        <div class="content-panel">
          <!-- Режим просмотра всех постов -->
          <div *ngIf="activeTab() === 'posts' && !selectedPostId()" class="empty-content">
            <p>Выберите пост для просмотра деталей и управления</p>
          </div>

          <ng-container *ngIf="activeTab() === 'posts' && selectedPostId() && currentPost()">
            <ng-container *ngIf="currentPost() as post">
              <div class="selected-content">
                <div class="content-header">
                  <h3>Пост: {{ post.title || 'Без заголовка' }}</h3>
                </div>

                <div class="post-details">
                  <div class="post-header">
                    <div class="post-meta">
                      <span>Автор: {{ post.creatorName || 'Неизвестно' }}</span>
                      <span [class.published]="post.isPublished" [class.unpublished]="!post.isPublished">
                        {{ post.isPublished ? 'Опубликован' : 'Не опубликован' }}
                      </span>
                      <span *ngIf="post.isShadowBanned" class="shadow-banned-badge">Теневой бан</span>
                    </div>
                  </div>
                  <div class="post-content">
                    <p>{{ post.text || 'Нет текста' }}</p>
                  </div>
                  <div class="post-stats">
                    <span>Лайки: {{ post.likesCount || 0 }}</span>
                    <span>Комментарии: {{ post.commentsCount || 0 }}</span>
                    <span>Медиа: {{ post.mediaCount || 0 }}</span>
                  </div>
                <div class="post-actions">
                  <button 
                    *ngIf="post.id && !post.isShadowBanned" 
                    (click)="shadowBanPost(post.id!)" 
                    class="shadow-ban-btn"
                  >
                    Теневой бан
                  </button>
                  <button 
                    *ngIf="post.id && post.isShadowBanned" 
                    (click)="unshadowBanPost(post.id!)" 
                    class="unshadow-ban-btn"
                  >
                    Снять т.бан
                  </button>
                </div>
                </div>
              </div>
            </ng-container>
          </ng-container>

          <!-- Режим просмотра контента пользователя/автора -->
          <div *ngIf="activeTab() !== 'posts' && !selectedAuthorId()" class="empty-content">
            <p>Выберите автора для просмотра контента</p>
          </div>

          <div *ngIf="activeTab() !== 'posts' && selectedAuthorId()" class="selected-content">
            <div class="content-header">
              <h3>
                {{ selectedAuthor() ? 'Автор: ' + selectedAuthor()?.name : '' }}
              </h3>
              <button *ngIf="selectedAuthor()" 
                      (click)="toggleAuthorShadowBan()"
                      [class.shadow-banned]="selectedAuthor()?.isShadowBanned"
                      class="shadow-ban-btn">
                {{ selectedAuthor()?.isShadowBanned ? 'Снять т.бан' : 'Теневой бан' }}
              </button>
            </div>

            <div *ngIf="loadingPosts()" class="loading">Загрузка постов...</div>
            <div *ngIf="postsError()" class="error">{{ postsError() }}</div>

            <div *ngIf="!loadingPosts() && selectedPosts().length > 0" class="posts-list">
              <div *ngFor="let post of selectedPosts()" class="post-item">
                <div class="post-header">
                  <h4>{{ post.title || 'Без заголовка' }}</h4>
                  <div class="post-meta">
                    <span [class.published]="post.isPublished" [class.unpublished]="!post.isPublished">
                      {{ post.isPublished ? 'Опубликован' : 'Не опубликован' }}
                    </span>
                    <span *ngIf="post.isShadowBanned" class="shadow-banned-badge">Теневой бан</span>
                  </div>
                </div>
                <div class="post-content">
                  <p>{{ post.text || 'Нет текста' }}</p>
                </div>
                <div class="post-stats">
                  <span>Лайки: {{ post.likesCount || 0 }}</span>
                  <span>Комментарии: {{ post.commentsCount || 0 }}</span>
                  <span>Медиа: {{ post.mediaCount || 0 }}</span>
                </div>
                <div class="post-actions">
                  <button 
                    *ngIf="post.id && !post.isShadowBanned" 
                    (click)="shadowBanPost(post.id!)" 
                    class="shadow-ban-btn"
                  >
                    Теневой бан
                  </button>
                  <button 
                    *ngIf="post.id && post.isShadowBanned" 
                    (click)="unshadowBanPost(post.id!)" 
                    class="unshadow-ban-btn"
                  >
                    Снять т.бан
                  </button>
                </div>
              </div>
            </div>

            <div *ngIf="!loadingPosts() && selectedPosts().length === 0" class="empty">
              У выбранного пользователя/автора нет постов
            </div>
          </div>
        </div>
      </div>

      <!-- Модальное окно подтверждения -->
      <div *ngIf="showConfirmModal()" class="confirm-modal-overlay" (click)="closeConfirmModal()">
        <div class="confirm-modal-content" (click)="$event.stopPropagation()">
          <h3>{{ confirmModalTitle() }}</h3>
          <p>{{ confirmModalMessage() }}</p>
          <div class="confirm-modal-actions">
            <button (click)="confirmAction()" class="confirm-btn">Подтвердить</button>
            <button (click)="closeConfirmModal()" class="cancel-btn">Отмена</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-dashboard {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      padding-bottom: 15px;
      border-bottom: 2px solid #e0e0e0;
    }

    .dashboard-header h1 {
      margin: 0;
    }

    .logout-btn {
      padding: 10px 20px;
      background: #f44336;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .logout-btn:hover {
      background: #d32f2f;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    }

    .search-section {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }

    .search-input {
      flex: 1;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 16px;
    }

    .content-wrapper {
      display: grid;
      grid-template-columns: 350px 1fr;
      gap: 20px;
      height: calc(100vh - 200px);
    }

    .users-panel {
      border: 1px solid #ddd;
      border-radius: 8px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }

    .tabs {
      display: flex;
      border-bottom: 1px solid #ddd;
      flex-wrap: wrap;
    }

    .tabs button {
      flex: 1;
      min-width: 100px;
      padding: 12px;
      border: none;
      background: #f5f5f5;
      cursor: pointer;
      font-weight: 500;
    }

    .tabs button.active {
      background: #007bff;
      color: white;
    }

    .users-list {
      flex: 1;
      overflow-y: auto;
      padding: 10px;
    }

    .user-item {
      padding: 12px;
      border: 1px solid #e0e0e0;
      border-radius: 6px;
      margin-bottom: 8px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .user-item:hover {
      background: #f5f5f5;
      border-color: #007bff;
    }

    .user-item.selected {
      background: #e3f2fd;
      border-color: #007bff;
    }

    .user-info {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .user-info strong {
      font-size: 16px;
    }

    .email {
      font-size: 12px;
      color: #666;
    }

    .type {
      font-size: 11px;
      color: #999;
      text-transform: uppercase;
    }

    .author-stats {
      display: flex;
      gap: 10px;
      font-size: 12px;
      color: #666;
      margin-top: 4px;
    }

    .shadow-banned-badge {
      display: inline-block;
      background: #ff9800;
      color: white;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 11px;
      margin-top: 4px;
    }

    .content-panel {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 20px;
      overflow-y: auto;
    }

    .empty-content {
      text-align: center;
      padding: 40px;
      color: #999;
    }

    .content-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      padding-bottom: 15px;
      border-bottom: 2px solid #e0e0e0;
    }

    .posts-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .post-item {
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      padding: 16px;
      background: #fafafa;
    }

    .post-header {
      display: flex;
      justify-content: space-between;
      align-items: start;
      margin-bottom: 12px;
    }

    .post-header h4 {
      margin: 0;
      flex: 1;
    }

    .post-meta {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .published {
      color: #4caf50;
      font-weight: 500;
    }

    .unpublished {
      color: #999;
    }

    .post-content {
      margin-bottom: 12px;
      color: #333;
      max-height: 100px;
      overflow-y: auto;
    }

    .post-stats {
      display: flex;
      gap: 16px;
      font-size: 14px;
      color: #666;
      margin-bottom: 12px;
    }

    .post-actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    button {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
    }

    button:hover {
      opacity: 0.9;
      transform: translateY(-1px);
    }

    .shadow-ban-btn {
      background: #ff9800;
      color: white;
    }

    .shadow-ban-btn.shadow-banned {
      background: #4caf50;
    }

    .unshadow-ban-btn {
      background: #4caf50;
      color: white;
    }

    .confirm-modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 10000;
    }

    .confirm-modal-content {
      background: white;
      padding: 24px;
      border-radius: 12px;
      min-width: 400px;
      max-width: 500px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
    }

    .confirm-modal-content h3 {
      margin: 0 0 16px 0;
      font-size: 20px;
      color: #333;
    }

    .confirm-modal-content p {
      margin: 0 0 24px 0;
      color: #666;
      line-height: 1.5;
    }

    .confirm-modal-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
    }

    .confirm-btn {
      padding: 10px 20px;
      background: #ff9800;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 500;
      transition: background 0.2s;
    }

    .confirm-btn:hover {
      background: #f57c00;
    }

    .cancel-btn {
      padding: 10px 20px;
      background: #e0e0e0;
      color: #333;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 500;
      transition: background 0.2s;
    }

    .cancel-btn:hover {
      background: #bdbdbd;
    }

    .loading, .error {
      text-align: center;
      padding: 20px;
    }

    .error {
      color: #f44336;
    }

    .empty {
      text-align: center;
      padding: 40px;
      color: #999;
    }

    .post-item-list {
      cursor: pointer;
    }

    .post-meta-list {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-top: 4px;
      flex-wrap: wrap;
    }

    .post-stats-list {
      display: flex;
      gap: 12px;
      font-size: 12px;
      color: #666;
      margin-top: 4px;
    }

    .post-details {
      padding: 16px;
      background: #fafafa;
      border-radius: 8px;
      border: 1px solid #e0e0e0;
    }
  `]
})
export class AdminDashboard implements OnInit {
  private adminFacade = inject(AdminFacade);
  private authFacade = inject(AuthFacade);
  private router = inject(Router);
  private sessionService = inject(SessionService);

  // Data
  authors = signal<AdminAuthor[]>([]);
  allPosts = signal<AdminContentPost[]>([]);
  
  // UI State
  loading = signal(false);
  error = signal<string | null>(null);
  loadingPosts = signal(false);
  postsError = signal<string | null>(null);
  activeTab = signal<'authors' | 'posts'>('authors');
  searchQuery = signal('');
  
  // Selection
  selectedAuthorId = signal<string | null>(null);
  selectedPostId = signal<string | null>(null);
  
  // Модальное окно подтверждения
  showConfirmModal = signal(false);
  confirmModalTitle = signal('');
  confirmModalMessage = signal('');
  confirmModalAction = signal<'shadowBanPost' | 'shadowBanAuthor' | null>(null);
  confirmModalTargetId = signal<string | null>(null);

  // Computed
  filteredAuthors = computed(() => {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.authors();
    return this.authors().filter(a => 
      (a.name || '').toLowerCase().includes(query) ||
      (a.email || '').toLowerCase().includes(query)
    );
  });

  filteredPosts = computed(() => {
    const query = this.searchQuery().trim();
    const posts = this.allPosts();
    
    console.log('🔍 filteredPosts computed:', { 
      query, 
      queryLength: query.length,
      postsCount: posts.length,
      posts: posts.map(p => ({ id: p.id, title: p.title }))
    });
    
    // Если запрос пустой, возвращаем все посты
    if (!query) {
      console.log('✅ No query, returning all posts');
      return posts;
    }
    
    const queryLower = query.toLowerCase();
    
    // Фильтруем посты по названию
    const result = posts.filter(p => {
      const title = p.title?.trim() || '';
      if (!title) {
        console.log(`  ❌ Post ${p.id}: no title`);
        return false;
      }
      
      const titleLower = title.toLowerCase();
      const matches = titleLower.includes(queryLower);
      console.log(`  ${matches ? '✅' : '❌'} Post "${title}": matches=${matches} (query="${queryLower}")`);
      return matches;
    });
    
    console.log('📊 Filter result:', { 
      query, 
      total: posts.length, 
      filtered: result.length,
      result: result.map(p => p.title)
    });
    
    return result;
  });

  selectedAuthor = computed(() => {
    const id = this.selectedAuthorId();
    if (!id) return null;
    return this.authors().find(a => a.creatorPageId === id) || null;
  });

  selectedPosts = computed(() => {
    const authorId = this.selectedAuthorId();
    
    if (!authorId) return [];
    
    // Фильтруем посты по creatorPageDataId выбранного автора
    return this.allPosts().filter(p => p.creatorPageDataId === authorId);
  });

  currentPost = computed(() => {
    const id = this.selectedPostId();
    if (!id) return null;
    return this.allPosts().find(p => p.id === id) || null;
  });

  ngOnInit() {
    // Guard уже проверил права доступа, поэтому просто загружаем данные
    this.loadAllData();
  }


  onSearchInput(value: string): void {
    this.searchQuery.set(value);
  }

  setTab(tab: 'authors' | 'posts') {
    this.activeTab.set(tab);
    this.selectedAuthorId.set(null);
    this.selectedPostId.set(null);
  }

  loadAllData() {
    this.loading.set(true);
    this.error.set(null);
    
    // Загружаем авторов и посты параллельно
    forkJoin({
      authors: this.adminFacade.getAllAuthors(),
      posts: this.adminFacade.getAllPosts()
    }).subscribe({
      next: ({ authors, posts }) => {
        this.authors.set(authors);
        this.allPosts.set(posts);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading data:', err);
        this.error.set(err.error?.message || 'Ошибка загрузки данных');
        this.loading.set(false);
      }
    });
  }

  selectAuthor(creatorPageId: string | undefined) {
    if (!creatorPageId) return;
    this.selectedAuthorId.set(creatorPageId);
  }

  selectPost(postId: string | undefined) {
    if (!postId) return;
    this.selectedPostId.set(postId);
  }

  openConfirmModal(action: 'shadowBanPost' | 'shadowBanAuthor', targetId: string, title: string, message: string) {
    this.confirmModalAction.set(action);
    this.confirmModalTargetId.set(targetId);
    this.confirmModalTitle.set(title);
    this.confirmModalMessage.set(message);
    this.showConfirmModal.set(true);
  }

  closeConfirmModal() {
    this.showConfirmModal.set(false);
    this.confirmModalAction.set(null);
    this.confirmModalTargetId.set(null);
    this.confirmModalTitle.set('');
    this.confirmModalMessage.set('');
  }

  confirmAction() {
    const action = this.confirmModalAction();
    const targetId = this.confirmModalTargetId();
    
    if (!action || !targetId) {
      this.closeConfirmModal();
      return;
    }

    if (action === 'shadowBanPost') {
      this.executeShadowBanPost(targetId);
    } else if (action === 'shadowBanAuthor') {
      this.executeShadowBanAuthor(targetId);
    }

    this.closeConfirmModal();
  }

  shadowBanPost(id: string) {
    this.openConfirmModal(
      'shadowBanPost',
      id,
      'Теневой бан поста',
      'Добавить пост в теневой бан? Пост не будет виден пользователям, но автор не узнает об этом.'
    );
  }

  executeShadowBanPost(id: string) {
    this.adminFacade.shadowBanPost(id).subscribe({
      next: (response) => {
        if (response?.success) {
          this.loadAllData();
        } else {
          this.error.set(response?.message || 'Ошибка теневого бана поста');
        }
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Ошибка теневого бана поста');
      }
    });
  }

  unshadowBanPost(id: string) {
    this.adminFacade.unshadowBanPost(id).subscribe({
      next: (response) => {
        if (response?.success) {
          this.loadAllData();
        } else {
          this.error.set(response?.message || 'Ошибка снятия теневого бана с поста');
        }
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Ошибка снятия теневого бана с поста');
      }
    });
  }

  toggleAuthorShadowBan() {
    const author = this.selectedAuthor();
    if (!author || !author.creatorPageId) return;
    
    if (author.isShadowBanned) {
      this.unshadowBanAuthor(author.creatorPageId);
    } else {
      this.shadowBanAuthor(author.creatorPageId);
    }
  }

  shadowBanAuthor(creatorPageId: string) {
    this.openConfirmModal(
      'shadowBanAuthor',
      creatorPageId,
      'Теневой бан автора',
      'Добавить автора в теневой бан? Автор не будет виден при поиске, но не узнает об этом.'
    );
  }

  executeShadowBanAuthor(creatorPageId: string) {
    this.adminFacade.shadowBanAuthor(creatorPageId).subscribe({
      next: (response) => {
        if (response?.success) {
          this.loadAllData();
        } else {
          this.error.set(response?.message || 'Ошибка теневого бана автора');
        }
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Ошибка теневого бана автора');
      }
    });
  }

  unshadowBanAuthor(creatorPageId: string) {
    this.adminFacade.unshadowBanAuthor(creatorPageId).subscribe({
      next: (response) => {
        if (response?.success) {
          this.loadAllData();
        } else {
          this.error.set(response?.message || 'Ошибка снятия теневого бана с автора');
        }
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Ошибка снятия теневого бана с автора');
      }
    });
  }

  logout(): void {
    this.authFacade.logout();
  }
}

