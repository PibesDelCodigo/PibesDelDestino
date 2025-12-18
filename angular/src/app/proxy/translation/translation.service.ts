import type { TranslateDto, TranslationResultDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TranslationService {
  apiName = 'Default';
  

  translate = (input: TranslateDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TranslationResultDto>({
      method: 'POST',
      url: '/api/app/translation/translate',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
