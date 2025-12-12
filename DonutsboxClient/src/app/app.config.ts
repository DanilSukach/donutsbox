import { ApplicationConfig, APP_INITIALIZER, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideApi } from './api/api-config.provider';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from '@app/core/interseptors/credentials.interceptor';
import { authRefreshInterceptor } from '@app/core/interseptors/auth-refresh.interceptor';
import { notFoundInterceptor } from '@app/core/interseptors/not-found.interceptor';
import { SessionService } from '@app/core/services/session.service';
import { catchError, firstValueFrom, of } from 'rxjs';
import { LucideAngularModule, LUCIDE_ICONS, LucideIconProvider, CircleCheckBig } from 'lucide-angular';
import {
  User,
  Camera,
  Newspaper,
  X,
  Menu,
  Settings,
  ChevronDown,
  ChevronRight,
  ChevronLeft,
  Lock,
  Mail,
  FileText,
  LogOut,
  Users,
  Sparkles,
  Inbox,
  Clock,
  Film,
  Music,
  Image,
  TriangleAlert,
  Plus,
  Pencil,
  EyeOff,
  Trash2,
  Loader,
  Bookmark,
  Search,
  Gem,
  Rocket,
  Check,
  Pin,
  Target,
  Upload,
  Video,
  ChevronUp,
  Mic,
  Folder,
  Save,
  MessageCircle,
  Play, 
  Pause, 
  Square,
  ThumbsUp,
  ThumbsDown
} from 'lucide-angular';

export function initSession(sessionService: SessionService) {
  return () =>
    firstValueFrom(sessionService.ensureSession().pipe(catchError(() => of(null))));
}


const myIcons = {
  User, 
  Camera, 
  Newspaper, 
  X, 
  Menu, 
  Settings, 
  ChevronDown,
  ChevronRight,
  ChevronLeft,
  ChevronUp,
  Lock, 
  Mail, 
  FileText,
  LogOut, 
  Users, 
  Sparkles, 
  Inbox, 
  Clock, 
  Film, 
  Music, 
  Image, 
  TriangleAlert, 
  Plus, 
  Pencil, 
  EyeOff, 
  Trash2, 
  Loader, 
  Bookmark, 
  Search, 
  Gem, 
  Rocket, 
  Check, 
  Upload, 
  Target, 
  Video, 
  Mic, 
  Folder, 
  Save,
  CircleCheckBig,
  Pin,
  MessageCircle, 
  Play, 
  Pause, 
  Square,
  ThumbsUp,
  ThumbsDown
};


export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes), provideClientHydration(withEventReplay()),
    provideHttpClient(withInterceptors([credentialsInterceptor, authRefreshInterceptor, notFoundInterceptor]), withFetch()),
    provideApi(),
    {provide: LUCIDE_ICONS, multi: true, useValue: new LucideIconProvider(myIcons)}
  ]
};
