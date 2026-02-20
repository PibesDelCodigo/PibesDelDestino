using Microsoft.Extensions.Configuration;
using PibesDelDestino.Cities;
using PibesDelDestino.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Microsoft.Extensions.Logging; 

namespace PibesDelDestino.GeoDb
{
    public class GeoDbCitySearchService : ICitySearchService, ITransientDependency
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<GeoDbCitySearchService> _logger;

        public GeoDbCitySearchService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IRepository<SearchHistory, Guid> searchHistoryRepo,
            IGuidGenerator guidGenerator, ILogger<GeoDbCitySearchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _searchHistoryRepo = searchHistoryRepo;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            var apiKey = _configuration["GeoDb:ApiKey"];
            var apiHost = "wft-geo-db.p.rapidapi.com";
            var client = _httpClientFactory.CreateClient();

            // Construcción segura de Query String
            var queryParams = new List<string> { "limit=5" };

            if (!string.IsNullOrWhiteSpace(request.PartialName))
            {
                // Uri.EscapeDataString convierte "New York" en "New%20York"
                queryParams.Add($"namePrefix={Uri.EscapeDataString(request.PartialName)}");
            }

            if (request.MinPopulation.HasValue)
            {
                queryParams.Add($"minPopulation={request.MinPopulation.Value}");
            }

            if (!string.IsNullOrWhiteSpace(request.CountryId))
            {
                queryParams.Add($"countryIds={Uri.EscapeDataString(request.CountryId)}");
            }

            var url = $"https://{apiHost}/v1/geo/cities?" + string.Join("&", queryParams);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("X-RapidAPI-Key", apiKey);
            httpRequest.Headers.Add("X-RapidAPI-Host", apiHost);

            try
            {
                using var response = await client.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"GeoDB API Error: {response.StatusCode}");
                    return new CityResultDto { Cities = new List<CityDto>() };
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<GeoDbApiResponse>();

                if (apiResponse?.Data == null)
                {
                    return new CityResultDto { Cities = new List<CityDto>() };
                }

                // Mapeo manual ya que es externo y no queremos acoplar DTOs internos a la estructura de la API
                var cityDtos = apiResponse.Data.Select(city => new CityDto
                {
                    Name = city.Name,
                    Country = city.Country,
                    Region = city.Region,
                    Latitude = city.Latitude,
                    Longitude = city.Longitude,
                    Population = city.Population
                }).ToList();

                // Guardado de historial asíncrono
                if (!string.IsNullOrWhiteSpace(request.PartialName))
                {
                    await SafeSaveHistoryAsync(request.PartialName, cityDtos.Count);
                }

                return new CityResultDto { Cities = cityDtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico conectando a GeoDB API");
                return new CityResultDto { Cities = new List<CityDto>() };
            }
        }

        // Método auxiliar para guardar historial sin romper el flujo principal si falla la BD
        private async Task SafeSaveHistoryAsync(string term, int resultsCount)
        {
            try
            {
                await _searchHistoryRepo.InsertAsync(new SearchHistory(
                    _guidGenerator.Create(),
                    term.Trim().ToLower(),
                    resultsCount
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar el historial de búsqueda.");
            }
        }
    }
}