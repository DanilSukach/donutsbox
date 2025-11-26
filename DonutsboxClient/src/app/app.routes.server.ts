import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Маршруты требующие авторизации - рендерятся только в браузере
  {
    path: 'feed',
    renderMode: RenderMode.Client
  },
  {
    path: 'profile/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'profile/setup',
    renderMode: RenderMode.Client
  },
  {
    path: 'profile/subscription-setup',
    renderMode: RenderMode.Client
  },
  {
    path: 'payments/result',
    renderMode: RenderMode.Client
  },
  // Публичные маршруты - пререндерятся при сборке
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
