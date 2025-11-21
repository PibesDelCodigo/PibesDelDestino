
export interface CityDto {
  name?: string;
  country?: string;
  region?: string;
  latitude: number;
  longitude: number;
}

export interface CityRequestDTO {
  partialName?: string;
}

export interface CityResultDto {
  cities: CityDto[];
}
