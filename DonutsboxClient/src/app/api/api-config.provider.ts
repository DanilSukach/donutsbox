import { importProvidersFrom } from '@angular/core';
import { ApiModule as DonutsboxApiModule } from './donutsbox';
import { ApiModule as AuthApiModule } from './auth';
import { Configuration, ConfigurationParameters } from './donutsbox/configuration';
import { Configuration as AuthConfiguration, ConfigurationParameters as AuthConfigurationParameters } from './auth/configuration';

export function donutsboxApiConfigFactory(): Configuration {
  const params: ConfigurationParameters = {
    basePath: 'https://localhost:7133',
  };
  return new Configuration(params);
}

export function authApiConfigFactory(): AuthConfiguration {
    const params: AuthConfigurationParameters = {
      basePath: 'https://localhost:7016', 
    };
    return new AuthConfiguration(params);
}

export function provideApi() {
  return importProvidersFrom(
    DonutsboxApiModule.forRoot(donutsboxApiConfigFactory),
    AuthApiModule.forRoot(authApiConfigFactory)
  );
}