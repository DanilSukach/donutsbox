import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import 'vidstack/player';
import 'vidstack/player/layouts';
import 'vidstack/player/ui';
bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
