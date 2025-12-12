import { importProvidersFrom } from '@angular/core';
import { ApiModule as DonutsboxApiModule } from './donutsbox';
import { ApiModule as AuthApiModule } from './auth';
import { ApiModule as AdminApiModule } from './admin';
import { Configuration, ConfigurationParameters } from './donutsbox/configuration';
import { Configuration as AuthConfiguration, ConfigurationParameters as AuthConfigurationParameters } from './auth/configuration';
import { Configuration as AdminConfiguration, ConfigurationParameters as AdminConfigurationParameters } from './admin/configuration';
import { environment } from '@env/environment';

export function donutsboxApiConfigFactory(): Configuration {
  const params: ConfigurationParameters = {
    basePath: environment.donutsboxApiBaseUrl,
    withCredentials: true
  };
  return new Configuration(params);
}

export function authApiConfigFactory(): AuthConfiguration {
    const params: AuthConfigurationParameters = {
      basePath: environment.authApiBaseUrl,
      withCredentials: true
    };
    return new AuthConfiguration(params);
}

export function adminApiConfigFactory(): AdminConfiguration {
    const params: AdminConfigurationParameters = {
      basePath: environment.adminApiBaseUrl || environment.donutsboxApiBaseUrl,
      withCredentials: true
    };
    return new AdminConfiguration(params);
}

export function provideApi() {
  return importProvidersFrom(
    DonutsboxApiModule.forRoot(donutsboxApiConfigFactory),
    AuthApiModule.forRoot(authApiConfigFactory),
    AdminApiModule.forRoot(adminApiConfigFactory)
  );
}