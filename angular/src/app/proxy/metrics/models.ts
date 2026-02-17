import type { AuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface ApiMetricDto extends AuditedEntityDto<string> {
  serviceName?: string;
  endpoint?: string;
  isSuccess: boolean;
  responseTimeMs: number;
  errorMessage?: string;
}

export interface DashboardDto {
  totalApiCalls: number;
  successRate: number;
  avgResponseTime: number;
  topSearches: Record<string, number>;
}

export interface GetApiMetricsInput extends PagedAndSortedResultRequestDto {
  serviceName?: string;
  startDate?: string;
  endDate?: string;
}
