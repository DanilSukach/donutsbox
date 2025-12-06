import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin-guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./pages/admin-dashboard/admin-dashboard').then((c) => c.AdminDashboard),
  },
  {
    path: '**',
    redirectTo: '/404',
  },
];

