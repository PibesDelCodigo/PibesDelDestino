import type { ApiMetricDto, DashboardDto, GetApiMetricsInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ApiMetricService {
  apiName = 'Default';
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ApiMetricDto>({
      method: 'GET',
      url: `/api/app/api-metric/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getDashboardStats = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DashboardDto>({
      method: 'GET',
      url: '/api/app/api-metric/dashboard-stats',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetApiMetricsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ApiMetricDto>>({
      method: 'GET',
      url: '/api/app/api-metric',
      params: { serviceName: input.serviceName, startDate: input.startDate, endDate: input.endDate, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
