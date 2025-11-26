using Microsoft.Extensions.Configuration;
using PibesDelDestino.Cities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PibesDelDestino.GeoDb
{
    public class GeoDbCitySearchService : ICitySearchService, ITransientDependency
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Inyectamos las dependencias necesarias: IHttpClientFactory y IConfiguration
        public GeoDbCitySearchService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            var apiUrl = _configuration["GeoDb:ApiUrl"]; // O la URL hardcodeada si la dejaste así por ahora
            var apiKey = _configuration["GeoDb:ApiKey"];

            var client = _httpClientFactory.CreateClient();

            // Ponemos la URL base completa y correcta "a fuego" (hardcoded)
            var url = "https://wft-geo-db.p.rapidapi.com/v1/geo/cities?limit=5";

            // Agregamos filtros dinámicamente
            if (!string.IsNullOrWhiteSpace(request.PartialName))
            {
                url += $"&namePrefix={request.PartialName}";
            }

            if (request.MinPopulation.HasValue)
            {
                url += $"&minPopulation={request.MinPopulation.Value}";
            }

            if (!string.IsNullOrWhiteSpace(request.CountryId))
            {
                url += $"&countryIds={request.CountryId}";
            }

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new System.Uri(url),
                Headers =
        {
            { "X-RapidAPI-Key", apiKey },
            { "X-RapidAPI-Host", "wft-geo-db.p.rapidapi.com" },
        },
            };
            try
            {
                using (var response = await client.SendAsync(httpRequest))
                {
                    // 1. Verificamos si falló, pero SUAVEMENTE
                    if (!response.IsSuccessStatusCode)
                    {
                        // Si es 404 (No encontrado) o 400 (Bad Request por poner UK), 
                        // devolvemos lista vacía en lugar de romper.
                        return new CityResultDto { Cities = new List<CityDto>() };
                    }

                    // 2. Si llegamos acá, es porque todo salió bien (200 OK)
                    var apiResponse = await response.Content.ReadFromJsonAsync<GeoDbApiResponse>();

                    if (apiResponse?.Data == null)
                    {
                        return new CityResultDto { Cities = new List<CityDto>() };
                    }

                    var cityDtos = apiResponse.Data.Select(city => new CityDto
                    {
                        Name = city.Name,
                        Country = city.Country,
                        Region = city.Region,
                        Latitude = city.Latitude,
                        Longitude = city.Longitude,
                        Population = city.Population
                    }).ToList();

                    return new CityResultDto { Cities = cityDtos };
                }
            }
            catch (Exception ex)
            {
                // 3. Si explota la conexión (se corta internet), caemos acá.
                // Devolvemos lista vacía para que el frontend no muestre pantalla roja.
                return new CityResultDto { Cities = new List<CityDto>() };
            }
        }
    }
}