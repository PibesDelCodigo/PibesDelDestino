import type { UserPreferencesDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  apiName = 'Default';
  

  getPreferences = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserPreferencesDto>({
      method: 'GET',
      url: '/api/app/settings/preferences',
    },
    { apiName: this.apiName,...config });
  

  updatePreferences = (input: UserPreferencesDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/settings/preferences',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
