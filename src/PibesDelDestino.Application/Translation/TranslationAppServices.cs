using Microsoft.Extensions.Logging;
using PibesDelDestino.Metrics;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Translation
{
    public class TranslationAppService : ApplicationService, ITranslationAppService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRepository<ApiMetric, Guid> _metricRepository;

        public TranslationAppService(
            IHttpClientFactory httpClientFactory,
            IRepository<ApiMetric, Guid> metricRepository)
        {
            _httpClientFactory = httpClientFactory;
            _metricRepository = metricRepository;
        }

        public async Task<TranslationResultDto> TranslateAsync(TranslateDto input)
        {
            var stopwatch = Stopwatch.StartNew();
            var isSuccess = false;
            string errorMessage = null;
            string translatedText = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input.TextToTranslate)}&langpair=es|{input.TargetLanguage}";

                var response = await client.GetAsync(url);
                isSuccess = response.IsSuccessStatusCode;

                if (isSuccess)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    translatedText = doc.RootElement
                        .GetProperty("responseData")
                        .GetProperty("translatedText")
                        .GetString();

                    return new TranslationResultDto { TranslatedText = translatedText };
                }
                else
                {
                    errorMessage = $"Error HTTP: {response.StatusCode}";
                    Logger.LogWarning($"⚠️ MyMemory API falló: {errorMessage}");
                    return new TranslationResultDto { TranslatedText = "Error en la traducción" };
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                Logger.LogError(ex, $"❌ Excepción en TranslationService: {ex.Message}");
                throw; // Re-lanzamos para que el llamador sepa que algo falló gravemente
            }
            finally
            {
                stopwatch.Stop();
                // Registro de métrica sin bloquear el resultado principal
                await SafeLogMetricAsync(isSuccess, stopwatch.ElapsedMilliseconds, errorMessage);
            }
        }

        private async Task SafeLogMetricAsync(bool isSuccess, long elapsedMs, string error)
        {
            try
            {
                await _metricRepository.InsertAsync(new ApiMetric(
                    GuidGenerator.Create(),
                    serviceName: "MyMemoryTranslationApi",
                    endpoint: "/get",
                    isSuccess: isSuccess,
                    responseTimeMs: (int)elapsedMs,
                    errorMessage: error ?? string.Empty
                ));
            }
            catch (Exception ex)
            {
                // Si falla la métrica, solo lo logueamos, no rompemos la traducción
                Logger.LogError(ex, "❌ No se pudo guardar la métrica de traducción.");
            }
        }
    }
}