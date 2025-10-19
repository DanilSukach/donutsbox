import { inject, Injectable, signal } from '@angular/core';
import { AuthorsService } from '@app/api/donutsbox';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { Observable, catchError, of, tap } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthorsFacade {
  private readonly authorsService = inject(AuthorsService);
  private readonly router = inject(Router);

  readonly authors = signal<AuthorRequestDto[]>([]);
  readonly isLoadingAuthors = signal(false);
  readonly authorsError = signal<string | null>(null);

  readonly selectedAuthor = signal<AuthorRequestDto | null>(null);
  readonly isLoadingAuthor = signal(false);

  loadAuthors(
    page: number = 1,
    pageSize: number = 20,
    sortBy?: string,
    descending: boolean = false
  ): Observable<AuthorRequestDto[]> {
    this.isLoadingAuthors.set(true);
    this.authorsError.set(null);

    return this.authorsService.apiAuthorsGet(page, pageSize, sortBy, descending).pipe(
      tap((authors) => {
        this.authors.set(authors);
        this.isLoadingAuthors.set(false);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки авторов:', error);
        this.authorsError.set('Не удалось загрузить авторов');
        this.isLoadingAuthors.set(false);
        return of([]);
      })
    );
  }

  loadAuthorById(id: string): Observable<AuthorRequestDto | null> {
    this.isLoadingAuthor.set(true);

    return this.authorsService.apiAuthorsIdGet(id).pipe(
      tap((author) => {
        this.selectedAuthor.set(author);
        this.isLoadingAuthor.set(false);
      }),
      catchError((error) => {
        console.error('Ошибка загрузки автора:', error);
        this.selectedAuthor.set(null);
        this.isLoadingAuthor.set(false);
        return of(null);
      })
    );
  }

  navigateToAuthor(authorId: string): void {
    this.router.navigate(['/profile', authorId]);
  }

  searchAuthors(query: string): Observable<AuthorRequestDto[]> {
    console.log('Поиск авторов по запросу:', query);
    return of([]);
  }

  
  getRecommendedAuthors(userId: string): Observable<AuthorRequestDto[]> {
    console.log('Загрузка рекомендованных авторов для пользователя:', userId);
    // Здесь будет логика получения рекомендаций
    return of([]);
  }


  clearState(): void {
    this.authors.set([]);
    this.selectedAuthor.set(null);
    this.isLoadingAuthors.set(false);
    this.isLoadingAuthor.set(false);
    this.authorsError.set(null);
  }
}
