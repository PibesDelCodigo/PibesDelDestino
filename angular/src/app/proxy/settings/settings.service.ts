import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  apiName = 'Default';
  

  getNotificationPreference = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'GET',
      url: '/api/app/settings/notification-preference',
    },
    { apiName: this.apiName,...config });
  

  updateNotificationPreference = (enabled: boolean, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/settings/notification-preference',
      params: { enabled },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
