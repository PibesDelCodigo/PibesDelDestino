import { ExtensibleObject } from '@abp/ng.core';

export interface UserProfileDto extends ExtensibleObject {
  userName: string;
  email: string;
  name: string;
  surname: string;
  phoneNumber: string;
  isExternal: boolean;
  hasPassword: boolean;
  concurrencyStamp: string;
}

export enum NotificationChannel {
  Email = 0,
  Screen = 1,
  Both = 2
}

export enum NotificationFrequency {
  Immediate = 0,
  WeeklySummary = 1
}