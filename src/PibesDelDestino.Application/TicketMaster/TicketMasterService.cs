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

namespace PibesDelDestino.TicketMaster
{
    public class TicketMasterService : ITicketMasterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketMasterService> _logger;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IGuidGenerator _guidGenerator;

        public TicketMasterService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TicketMasterService> logger, IRepository<SearchHistory,
            Guid> searchHistoryRepo,
            IGuidGenerator guidGenerator)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _searchHistoryRepo = searchHistoryRepo;
            _guidGenerator = guidGenerator;
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

            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"⚠️ TicketMaster respondió con error: {response.StatusCode}");
                    return new List<EventoDTO>();
                }

                var root = await response.Content.ReadFromJsonAsync<TicketMasterRoot>();

                if (root?.Embedded?.Events == null)
                {
                    return new List<EventoDTO>();
                }

                var cleanList = root.Embedded.Events.Select(e => new EventoDTO
                {
                    Name = e.Name,
                    Url = e.Url,
                    Date = e.Dates?.Start?.LocalDate ?? "Fecha no disponible",
                    ImageUrl = e.Images?.FirstOrDefault()?.Url
                }).ToList();

                await SaveMetricAsync(cityName, cleanList.Count);

                return cleanList;

            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error conectando a TicketMaster: {ex.Message}");
                return new List<EventoDTO>();
            }
        }

        private async Task SaveMetricAsync(string term, int count)
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