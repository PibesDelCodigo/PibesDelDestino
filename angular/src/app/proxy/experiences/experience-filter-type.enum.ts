import { mapEnumToOptions } from '@abp/ng.core';

export enum ExperienceFilterType {
  Positive = 0,
  Neutral = 1,
  Negative = 2,
}

export const experienceFilterTypeOptions = mapEnumToOptions(ExperienceFilterType);
