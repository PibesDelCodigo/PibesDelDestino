using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PibesDelDestino.Metrics;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using System.Diagnostics;

namespace PibesDelDestino.TicketMaster
{
    public class TicketMasterService : ITicketMasterService
    { 
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketMasterService> _logger;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IRepository<ApiMetric, Guid> _apiMetricRepo;

        public TicketMasterService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TicketMasterService> logger, IRepository<SearchHistory,
            Guid> searchHistoryRepo,
            IGuidGenerator guidGenerator, IRepository<ApiMetric, Guid> apiMetricRepo)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _searchHistoryRepo = searchHistoryRepo;
            _guidGenerator = guidGenerator;
            _apiMetricRepo = apiMetricRepo;
        }

        // Este método se encarga de buscar eventos en TicketMaster para una ciudad específica.
        public async Task<List<EventoDTO>> SearchEventsAsync(string cityName)
        {
            var apiKey = _configuration["TicketMaster:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("❌ La API Key de TicketMaster no está configurada.");
                return new List<EventoDTO>();
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://app.ticketmaster.com/discovery/v2/events.json?apikey={apiKey}&city={cityName}&sort=date,asc&size=5";

            var stopwatch = Stopwatch.StartNew();
            var isSuccess = false;
            string errorMessage = null;

            // Intentamos hacer la solicitud a TicketMaster y procesar la respuesta.
            try
            {
                var response = await client.GetAsync(url);
                isSuccess = response.IsSuccessStatusCode;

                if (!isSuccess)
                {
                    // Si la respuesta no es exitosa, registramos el error y devolvemos una lista vacía.
                    errorMessage = $"Error {response.StatusCode}";
                    _logger.LogWarning($"⚠️ TicketMaster respondió con error: {response.StatusCode}");
                    return new List<EventoDTO>();
                }

                // Si la respuesta es exitosa, intentamos deserializar el contenido.
                var root = await response.Content.ReadFromJsonAsync<TicketMasterRoot>();

                if (root?.Embedded?.Events == null)
                {
                    await SaveSearchHistoryAsync(cityName, 0);
                    return new List<EventoDTO>();
                }

                // Mapeamos los eventos obtenidos a una lista de EventoDTO,
                // asegurándonos de manejar posibles valores nulos.
                var cleanList = root.Embedded.Events.Select(e => new EventoDTO
                {
                    Name = e.Name,
                    Url = e.Url,
                    Date = e.Dates?.Start?.LocalDate ?? "Fecha no disponible",
                    ImageUrl = e.Images?.FirstOrDefault()?.Url
                }).ToList();

                await SaveSearchHistoryAsync(cityName, cleanList.Count);

                return cleanList;

            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                _logger.LogError($"❌ Error conectando a TicketMaster: {ex.Message}");
                return new List<EventoDTO>();
            }

            //Al finalizar la operación, guardamos una métrica con el resultado de la llamada a la API,
            //incluyendo el tiempo de respuesta y cualquier error ocurrido.
            finally
            {
                stopwatch.Stop();
                var metric = new ApiMetric(
                    _guidGenerator.Create(),           
                    "TicketMasterApi",                 
                    "/discovery/v2/events",            
                    isSuccess,                         
                    (int)stopwatch.ElapsedMilliseconds,
                    errorMessage ?? ""                  
                );

                await _apiMetricRepo.InsertAsync(metric, autoSave: true);

                _logger.LogInformation($"📊 Métrica guardada: TicketMasterApi ({stopwatch.ElapsedMilliseconds}ms)");
            }    
        }

        // Este método privado se encarga de guardar el término de búsqueda y
        // la cantidad de resultados obtenidos en el historial de búsquedas.
        private async Task SaveSearchHistoryAsync(string term, int count)
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                await _searchHistoryRepo.InsertAsync(new SearchHistory(
                    _guidGenerator.Create(),
                    term.Trim().ToLower(), 
                    count
                ), autoSave: true);
            }
        }
    }
}