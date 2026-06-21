using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PibesDelDestino.Metrics;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids; // Necesario para IGuidGenerator

namespace PibesDelDestino.TicketMaster
{
    // Implementa la interfaz y se registra como Transient
    public class TicketMasterEventSearchService : IEventSearchService, ITransientDependency
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IRepository<ApiMetric, Guid> _apiMetricRepo;
        private readonly ILogger<TicketMasterEventSearchService> _logger;
        private readonly IGuidGenerator _guidGenerator;

        public TicketMasterEventSearchService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IRepository<SearchHistory, Guid> searchHistoryRepo,
            IRepository<ApiMetric, Guid> apiMetricRepo,
            ILogger<TicketMasterEventSearchService> logger,
            IGuidGenerator guidGenerator)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _searchHistoryRepo = searchHistoryRepo;
            _apiMetricRepo = apiMetricRepo;
            _logger = logger;
            _guidGenerator = guidGenerator;
        }

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
            List<EventoDTO> resultList = new List<EventoDTO>();

            try
            {
                // Llamada a la API Externa
                var response = await client.GetAsync(url);
                isSuccess = response.IsSuccessStatusCode;

                if (!isSuccess)
                {
                    errorMessage = $"Error HTTP {response.StatusCode}";
                    _logger.LogWarning($"⚠️ TicketMaster respondió con error: {response.StatusCode}");
                    return resultList;
                }

                // Deserialización y Mapeo
                var root = await response.Content.ReadFromJsonAsync<TicketMasterRoot>();

                if (root?.Embedded?.Events != null)
                {
                    resultList = root.Embedded.Events.Select(e => new EventoDTO
                    {
                        Name = e.Name,
                        Url = e.Url,
                        Date = e.Dates?.Start?.LocalDate ?? "Fecha no disponible",
                        ImageUrl = e.Images?.FirstOrDefault()?.Url
                    }).ToList();
                }

                // Guardar Historial
                await SaveSearchHistoryAsync(cityName, resultList.Count);

                return resultList;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                _logger.LogError(ex, $"❌ Excepción conectando a TicketMaster: {ex.Message}");
                return new List<EventoDTO>();
            }
            finally
            {
                // Guardar Métricas
                stopwatch.Stop();
                await SafeLogMetricAsync(isSuccess, stopwatch.ElapsedMilliseconds, errorMessage);
            }
        }

        private async Task SaveSearchHistoryAsync(string term, int count)
        {
            if (string.IsNullOrWhiteSpace(term)) return;

            var history = new SearchHistory(
                _guidGenerator.Create(),
                term.Trim().ToLower(),
                count
            );

            await _searchHistoryRepo.InsertAsync(history);
        }

        private async Task SafeLogMetricAsync(bool isSuccess, long elapsedMs, string error)
        {
            try
            {
                var metric = new ApiMetric(
                    _guidGenerator.Create(),
                    "TicketMasterApi",
                    "/discovery/v2/events",
                    isSuccess,
                    (int)elapsedMs,
                    error ?? ""
                );
                await _apiMetricRepo.InsertAsync(metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al intentar guardar la métrica de API.");
            }
        }
    }
}