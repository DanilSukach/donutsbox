import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((r) => r.AUTH_ROUTES),
  },
  {
    path: 'profile/:id',
    loadComponent: () => import('./features/profile/pages/profile-page/profile-page').then((c) => c.ProfilePage)
  },
  {
    path: '',
    redirectTo: 'auth',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];
