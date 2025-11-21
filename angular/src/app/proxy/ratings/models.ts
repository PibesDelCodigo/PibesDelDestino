import type { AuditedEntityDto } from '@abp/ng.core';

export interface CreateRatingDto {
  destinationId: string;
  score: number;
  comment?: string;
}

export interface RatingDto extends AuditedEntityDto<string> {
  destinationId?: string;
  userId?: string;
  score: number;
  comment?: string;
}
