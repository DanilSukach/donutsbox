import { ApplicationConfig, APP_INITIALIZER, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideApi } from './api/api-config.provider';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from '@app/core/interseptors/credentials.interceptor';
import { authRefreshInterceptor } from '@app/core/interseptors/auth-refresh.interceptor';
import { SessionService } from '@app/core/services/session.service';
import { catchError, firstValueFrom, of } from 'rxjs';

export function initSession(sessionService: SessionService) {
  return () =>
    firstValueFrom(sessionService.ensureSession().pipe(catchError(() => of(null))));
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes), provideClientHydration(withEventReplay()),
    provideHttpClient(withInterceptors([credentialsInterceptor, authRefreshInterceptor]), withFetch()),
    provideApi(),
    {
      provide: APP_INITIALIZER,
      useFactory: initSession,
      deps: [SessionService],
      multi: true
    }
  ]
};
