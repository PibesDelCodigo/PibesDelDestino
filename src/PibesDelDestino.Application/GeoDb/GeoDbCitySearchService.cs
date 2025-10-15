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
            // 1. Obtenemos la configuración desde appsettings.json
            var apiUrl = "https://wft-geo-db.p.rapidapi.com";
            var apiKey = "1b87288382msh04081de1250362fp1acf94jsn6c66e7e31d14"; // Tu clave de API

            // 2. Creamos un cliente HTTP y preparamos la petición
            var client = _httpClientFactory.CreateClient();
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new System.Uri($"{apiUrl}/v1/geo/cities?namePrefix={request.PartialName}&limit=5"),
                Headers =
                {
                    { "X-RapidAPI-Key", apiKey },
                    { "X-RapidAPI-Host", "wft-geo-db.p.rapidapi.com" },
                },
            };

            // 3. Enviamos la petición y procesamos la respuesta
            using (var response = await client.SendAsync(httpRequest))
            {
                response.EnsureSuccessStatusCode();
                var apiResponse = await response.Content.ReadFromJsonAsync<GeoDbApiResponse>();

                if (apiResponse?.Data == null)
                {
                    return new CityResultDto { Cities = new List<CityDto>() };
                }

                // 4. Mapeamos la respuesta de la API a nuestro DTO de aplicación
                var cityDtos = apiResponse.Data.Select(city => new CityDto
                {
                    Name = city.Name,
                    Country = city.Country,
                    Region = city.Region,
                    Latitude = city.Latitude,
                    Longitude = city.Longitude
                }).ToList();

                return new CityResultDto { Cities = cityDtos };
            }
        }
    }
}