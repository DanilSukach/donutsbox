import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SessionService } from '@app/core/services/session.service';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './not-found-page.html',
  styleUrl: './not-found-page.css'
})
export class NotFoundPage implements OnInit {
  private sessionService = inject(SessionService);
  
  isAuthenticated = signal(false);
  userId = signal<string | null>(null);

  ngOnInit(): void {
    const session = this.sessionService.session();
    this.isAuthenticated.set(!!session);
    this.userId.set(session?.userId ?? null);
  }
}
