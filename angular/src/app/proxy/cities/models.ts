
export interface CityDto {
  name?: string;
  country?: string;
  region?: string;
  latitude: number;
  longitude: number;
  population: number;
}

export interface CityRequestDTO {
  partialName?: string;
  minPopulation?: number;
  countryId?: string;
}

export interface CityResultDto {
  cities: CityDto[];
}
