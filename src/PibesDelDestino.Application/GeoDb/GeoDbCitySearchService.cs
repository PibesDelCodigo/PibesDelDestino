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

namespace PibesDelDestino.GeoDb
{
    public class GeoDbCitySearchService : ICitySearchService, ITransientDependency
    {
        // Inyectamos el HttpClientFactory para hacer las llamadas a la API, IConfiguration para leer la configuración,
        // el repositorio de SearchHistory para guardar el historial de búsquedas, y el IGuidGenerator para generar IDs únicos.
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IGuidGenerator _guidGenerator;

        public GeoDbCitySearchService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IRepository<SearchHistory, Guid> searchHistoryRepo, // 👈 Inyectamos Repo
            IGuidGenerator guidGenerator)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _searchHistoryRepo = searchHistoryRepo;
            _guidGenerator = guidGenerator;
        }

        // Este método se encarga de buscar ciudades utilizando la API de GeoDB. Construye la URL con los parámetros de búsqueda,
        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            var apiUrl = _configuration["GeoDb:ApiUrl"];
            var apiKey = _configuration["GeoDb:ApiKey"];

            var client = _httpClientFactory.CreateClient();

            // Construimos la URL de la API con los parámetros de búsqueda. Si el usuario ha proporcionado un nombre parcial,
            // una población mínima o un ID de país, los añadimos a la URL.

            // La URL base es "https://wft-geo-db.p.rapidapi.com/v1/geo/cities?limit=5", y luego añadimos los parámetros según corresponda.
            var url = "https://wft-geo-db.p.rapidapi.com/v1/geo/cities?limit=5";

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

            // Construimos la solicitud HTTP con los encabezados necesarios para autenticarnos con la API de RapidAPI.
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
                    if (!response.IsSuccessStatusCode)
                    {
                        //Si la respuesta no es exitosa devolvemos una lista vacia para evitar errores
                        return new CityResultDto { Cities = new List<CityDto>() };
                    }

                    // Si la respuesta es exitosa, leemos el contenido y lo deserializamos en un objeto GeoDbApiResponse.
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

                    if (!string.IsNullOrWhiteSpace(request.PartialName))
                    {
                        // Guardamos el término de búsqueda en el historial solo si se ha proporcionado un nombre parcial.
                        // Esto nos permite tener un registro de las búsquedas realizadas por los usuarios.
                        await _searchHistoryRepo.InsertAsync(new SearchHistory(
                            _guidGenerator.Create(),
                            request.PartialName.Trim().ToLower(), // Guardamos "madrid", no "MaDrId"
                            cityDtos.Count
                        ));
                    }
                    return new CityResultDto { Cities = cityDtos };
                }
            }

            // En caso de cualquier excepción (problemas de red, errores de deserialización, etc.), capturamos la excepción y devolvemos una lista vacía de ciudades.
            catch (Exception ex)
            {
                return new CityResultDto { Cities = new List<CityDto>() };
            }
        }
    }
}