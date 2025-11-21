import type { AuditedEntityDto } from '@abp/ng.core';

export interface CoordinatesDto {
  latitude: number;
  longitude: number;
}

export interface CreateUpdateDestinationDto {
  name: string;
  country: string;
  city: string;
  population: number;
  photo?: string;
  updateDate: string;
  coordinates: CoordinatesDto;
}

export interface DestinationDto extends AuditedEntityDto<string> {
  name?: string;
  country?: string;
  city?: string;
  population: number;
  photo?: string;
  updateDate?: string;
  coordinates: CoordinatesDto;
}
