using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using PibesDelDestino.Metrics;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.TicketMaster
{
    public class TicketMasterService : ApplicationService, ITicketMasterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IRepository<ApiMetric, Guid> _apiMetricRepo;

        public TicketMasterService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IRepository<SearchHistory, Guid> searchHistoryRepo,
            IRepository<ApiMetric, Guid> apiMetricRepo)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _searchHistoryRepo = searchHistoryRepo;
            _apiMetricRepo = apiMetricRepo;
        }

        public async Task<List<EventoDTO>> SearchEventsAsync(string cityName)
        {
           
            var apiKey = _configuration["TicketMaster:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Logger.LogError("❌ La API Key de TicketMaster no está configurada.");
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
                //Llamada a la API Externa
                var response = await client.GetAsync(url);
                isSuccess = response.IsSuccessStatusCode;

                if (!isSuccess)
                {
                    errorMessage = $"Error HTTP {response.StatusCode}";
                    Logger.LogWarning($"⚠️ TicketMaster respondió con error: {response.StatusCode}");
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
                        // Null-conditional operators para evitar crash si faltan datos
                        Date = e.Dates?.Start?.LocalDate ?? "Fecha no disponible",
                        ImageUrl = e.Images?.FirstOrDefault()?.Url
                    }).ToList();
                }

                //Guardar Historial (Solo si fue exitoso)
                await SaveSearchHistoryAsync(cityName, resultList.Count);

                return resultList;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                Logger.LogError(ex, $"❌ Excepción conectando a TicketMaster: {ex.Message}");
                return new List<EventoDTO>();
            }
            finally
            {
                //Guardar Métricas
                stopwatch.Stop();
                await SafeLogMetricAsync(isSuccess, stopwatch.ElapsedMilliseconds, errorMessage);
            }
        }

        // Método auxiliar para guardar historial 
        private async Task SaveSearchHistoryAsync(string term, int count)
        {
            if (string.IsNullOrWhiteSpace(term)) return;

            var history = new SearchHistory(
                GuidGenerator.Create(),
                term.Trim().ToLower(),
                count
            );

            await _searchHistoryRepo.InsertAsync(history);
        }

        // Método auxiliar para guardar métricas sin romper el flujo principal
        private async Task SafeLogMetricAsync(bool isSuccess, long elapsedMs, string error)
        {
            try
            {
                var metric = new ApiMetric(
                    GuidGenerator.Create(),
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
                Logger.LogError(ex, "❌ Error al intentar guardar la métrica de API.");
            }
        }
    }
}