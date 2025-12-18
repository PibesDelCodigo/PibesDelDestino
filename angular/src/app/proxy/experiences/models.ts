import type { AuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { ExperienceFilterType } from './experience-filter-type.enum';

export interface CreateUpdateTravelExperienceDto {
  destinationId: string;
  title: string;
  description: string;
  date: string;
  rating: number;
}

export interface GetTravelExperiencesInput extends PagedAndSortedResultRequestDto {
  destinationId?: string;
  filterText?: string;
  type?: ExperienceFilterType;
  userId?: string;
}

export interface TravelExperienceDto extends AuditedEntityDto<string> {
  userId?: string;
  userName?: string;
  destinationId?: string;
  title?: string;
  description?: string;
  date?: string;
  rating: number;
}
