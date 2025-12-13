import type { CreateUpdateTravelExperienceDto, GetTravelExperiencesInput, TravelExperienceDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TravelExperienceService {
  apiName = 'Default';
  

  create = (input: CreateUpdateTravelExperienceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelExperienceDto>({
      method: 'POST',
      url: '/api/app/travel-experience',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/travel-experience/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelExperienceDto>({
      method: 'GET',
      url: `/api/app/travel-experience/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTravelExperiencesInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TravelExperienceDto>>({
      method: 'GET',
      url: '/api/app/travel-experience',
      params: { destinationId: input.destinationId, filterText: input.filterText, type: input.type, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTravelExperienceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelExperienceDto>({
      method: 'PUT',
      url: `/api/app/travel-experience/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
