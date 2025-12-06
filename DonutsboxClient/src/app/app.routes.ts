import { Routes } from '@angular/router';
import { newCreatorGuard } from './core/guards/new-creator-guard';
import { guestOnlyGuard } from './core/guards/guest-only-guard';
import { authGuard } from './core/guards/auth-guard';
import { creatorGuard } from './core/guards/creator-guard';


export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((r) => r.AUTH_ROUTES),
  },
  {
    path: 'profile/setup',
    loadComponent: () =>
      import('./features/profile/pages/profile-setup/profile-setup').then((c) => c.ProfileSetup),
    canActivate: [newCreatorGuard],
  },
  {
    path: 'profile/subscription-setup',
    loadComponent: () =>
      import('./features/profile/pages/subscription-setup/subscription-setup').then((c) => c.SubscriptionSetup),
    canActivate: [creatorGuard],
  },
  {
    path: 'profile/:id',
    loadComponent: () => import('./features/profile/pages/profile-page/profile-page').then((c) => c.ProfilePage),
    canActivate: [authGuard],
  },
  {
    path: 'feed',
    loadComponent: () => import('./features/feed/pages/feed-page/feed-page').then((c) => c.FeedPage),
    canActivate: [authGuard],
  },
  {
    path: 'management',
    canActivate: [authGuard],
    loadChildren: () => import('./features/admin/admin.routes').then((r) => r.ADMIN_ROUTES),
  },
  {
    path: 'payments/result',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/payments/pages/payment-result/payment-result').then((c) => c.PaymentResult),
  },
  {
    path: '',
    canActivate: [guestOnlyGuard],
    loadComponent: () => import('./features/home/pages/home-page/home-page').then((c) => c.HomePage),
  },
  {
    path: '404',
    loadComponent: () => import('./features/errors/pages/not-found-page/not-found-page').then((c) => c.NotFoundPage),
  },
  {
    path: '**',
    loadComponent: () => import('./features/errors/pages/not-found-page/not-found-page').then((c) => c.NotFoundPage),
  },
];
