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

        // Inyectamos HttpClientFactory (forma de conectarse a internet)=
        // y IConfiguration (claves secretas) para acceder
        // a la API externa y a las configuraciones.
        public GeoDbCitySearchService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        // REQUERIMIENTO 2.1 y 2.2: Buscar ciudades (Nombre y Filtros).
        // Este servicio se conecta a la API externa de GeoDB.
        // Implementa la búsqueda por nombre y aplica filtros dinámicos 
        // (País, Región, Población mínima) antes de devolver los resultados al front.
        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {//buscamos en appsettings.json la url y la clave de la API, para no
            //harcodearlas, si mañana la API cambia, solo tenemos que actualizar
            //el appsettings.json y no el codigo.
            var apiUrl = _configuration["GeoDb:ApiUrl"];
            //Nunca se escriben las claves directamente en el codigo, si subís
            //eso a gitHub, te roban la cuenta. La solucion es leerlas desde
            //appsettings.json o variables de entorno.
            var apiKey = _configuration["GeoDb:ApiKey"];


            //No hacemos new HttpClient() porque eso deja conexiones abiertas
            //(Sockets) y puede voltear el servidor si hay mucho tráfico. Usamos
            //CreateClient() para obtener una instancia optimizada y gestionada
            //por .NET Core.
            var client = _httpClientFactory.CreateClient();

            //Define la dirección exacta a la que vas a llamar.
            var url = "https://wft-geo-db.p.rapidapi.com/v1/geo/cities?limit=5";

            //REQ 2.1: Búsqueda por nombre (parcial)
            //Estás concatenando texto (+=). Tu URL base era: .../cities?limit=5.
            //Si el usuario escribe "Bar", la URL queda:
            //.../cities?limit=5&namePrefix=Bar
            if (!string.IsNullOrWhiteSpace(request.PartialName))
            {
                url += $"&namePrefix={request.PartialName}";
            }
            //REQ 2.2: Filtros dinámicos
            if (request.MinPopulation.HasValue)
            {
                url += $"&minPopulation={request.MinPopulation.Value}";
            }

            if (!string.IsNullOrWhiteSpace(request.CountryId))
            {
                url += $"&countryIds={request.CountryId}";
            }

            //Objeto mensaje
            var httpRequest = new HttpRequestMessage
            {//consulta de lectura al servidor, no de escritura, por eso es GET.
                Method = HttpMethod.Get,
            // La URL que armamos antes se la pasamos con los filtros pegados.
                RequestUri = new System.Uri(url),
                Headers =
        {//autenticacion, si no pasas esto rebota con error 403 Forbidden.
        //ApiKey es la contraseña que te da la API externa para identificarte,
        //es como tu DNI digital para acceder a sus datos.
            { "X-RapidAPI-Key", apiKey },
            //es para saber que API de RapidApi queres llamar
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

                    // 2. todo salió bien (200 OK)
                    //Lee el chorro de texto que mandó la API y lo convierte
                    //mágicamente en tu clase C# (GeoDbApiResponse) que definimos antes.
                    var apiResponse = await response.Content.ReadFromJsonAsync<GeoDbApiResponse>();

                    //validacion porque puede responder ok la API pero venir un campo vacio
                    if (apiResponse?.Data == null)
                    {
                        return new CityResultDto { Cities = new List<CityDto>() };
                    }

                    //los objertos sucios de la API externa los convierte en CityDTo
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
            //esta excepcion es para cualquier error de conexión, timeout, etc. que pueda ocurrir
            catch (Exception ex)
            {
                // 3. Si explota la conexión (se corta internet), caemos acá.
                // Devolvemos lista vacía para que el frontend no muestre pantalla roja.
                return new CityResultDto { Cities = new List<CityDto>() };
            }
        }
    }
}