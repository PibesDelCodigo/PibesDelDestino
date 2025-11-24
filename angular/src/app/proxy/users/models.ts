import type { EntityDto } from '@abp/ng.core';

export interface PublicUserDto extends EntityDto<string> {
  userName?: string;
  name?: string;
  surname?: string;
}
