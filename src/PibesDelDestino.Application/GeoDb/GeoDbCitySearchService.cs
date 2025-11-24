using Microsoft.Extensions.Configuration;
using PibesDelDestino.Cities;
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

            // Construimos la URL base
            var url = $"{apiUrl}/v1/geo/cities?limit=5";

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

            // ... (El resto del código de envío y respuesta queda igual) ...
            using (var response = await client.SendAsync(httpRequest))
            {
                // ... lógica de respuesta existente ...
                response.EnsureSuccessStatusCode();
                var apiResponse = await response.Content.ReadFromJsonAsync<GeoDbApiResponse>();
                // ... mapeo y retorno ...
                if (apiResponse?.Data == null) return new CityResultDto { Cities = new List<CityDto>() };

                var cityDtos = apiResponse.Data.Select(city => new CityDto
                {
                    Name = city.Name,
                    Country = city.Country,
                    Region = city.Region,
                    Latitude = city.Latitude,
                    Longitude = city.Longitude,
                    Population = city.Population,
                }).ToList();

                return new CityResultDto { Cities = cityDtos };
            }
        }
    }
}