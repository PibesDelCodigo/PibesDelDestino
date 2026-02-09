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

        public async Task<List<EventoDTO>> SearchEventsAsync(string cityName)
        {
            // 1. Leemos la clave de la configuración (appsettings.json)
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

            try
            {
                var response = await client.GetAsync(url);
                isSuccess = response.IsSuccessStatusCode;

                if (!isSuccess)
                {
                    errorMessage = $"Error {response.StatusCode}";
                    _logger.LogWarning($"⚠️ TicketMaster respondió con error: {response.StatusCode}");
                    return new List<EventoDTO>();
                }

                var root = await response.Content.ReadFromJsonAsync<TicketMasterRoot>();

                if (root?.Embedded?.Events == null)
                {
                    await SaveSearchHistoryAsync(cityName, 0);
                    return new List<EventoDTO>();
                }

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

            finally
            {
                stopwatch.Stop();

                // ✅ CORRECCIÓN: Usamos el constructor
                var metric = new ApiMetric(
                    _guidGenerator.Create(),           // 1. ID
                    "TicketMasterApi",                 // 2. ServiceName
                    "/discovery/v2/events",            // 3. Endpoint
                    isSuccess,                         // 4. IsSuccess
                    (int)stopwatch.ElapsedMilliseconds,// 5. ResponseTimeMs
                    errorMessage ?? ""                      // 6. ErrorMessage (puede ser null)
                );

                await _apiMetricRepo.InsertAsync(metric, autoSave: true);

                _logger.LogInformation($"📊 Métrica guardada: TicketMasterApi ({stopwatch.ElapsedMilliseconds}ms)");
            }    
        }

        private async Task SaveSearchHistoryAsync(string term, int count)
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                await _searchHistoryRepo.InsertAsync(new SearchHistory(
                    _guidGenerator.Create(),
                    term.Trim().ToLower(), // Guardamos "madrid"
                    count
                ), autoSave: true); // 👈 ¡CLAVE PARA QUE GUARDE AL INSTANTE!
            }
        }
    }
}