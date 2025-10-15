import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44304/',
  redirectUri: baseUrl,
  clientId: 'PibesDelDestino_App',
  responseType: 'code',
  scope: 'offline_access PibesDelDestino',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'PibesDelDestino',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44304',
      rootNamespace: 'PibesDelDestino',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;
