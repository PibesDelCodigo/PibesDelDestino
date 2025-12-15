import type { EntityDto } from '@abp/ng.core';

export interface AppNotificationDto extends EntityDto<string> {
  title?: string;
  message?: string;
  isRead: boolean;
  creationTime?: string;
}
