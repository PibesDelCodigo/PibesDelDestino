import type { CreateFavoriteDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { DestinationDto } from '../application/contracts/destinations/models';

@Injectable({
  providedIn: 'root',
})
export class FavoriteService {
  apiName = 'Default';
  

  getMyFavorites = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DestinationDto[]>({
      method: 'GET',
      url: '/api/app/favorite/my-favorites',
    },
    { apiName: this.apiName,...config });
  

  isFavorite = (input: CreateFavoriteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: '/api/app/favorite/is-favorite',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  toggle = (input: CreateFavoriteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: '/api/app/favorite/toggle',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
