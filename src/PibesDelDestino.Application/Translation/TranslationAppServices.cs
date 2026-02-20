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
            var isSuccess = false; // Empieza en falso
            string errorMessage = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input.TextToTranslate)}&langpair=es|{input.TargetLanguage}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    // 1. REVISAR EL STATUS INTERNO DE LA API
                    // MyMemory a veces manda un 200 OK pero el "responseStatus" interno es un error
                    var responseStatus = doc.RootElement.GetProperty("responseStatus");
                    int apiStatusCode = responseStatus.ValueKind == JsonValueKind.Number
                        ? responseStatus.GetInt32()
                        : int.Parse(responseStatus.GetString() ?? "0");

                    if (apiStatusCode == 200)
                    {
                        var translatedText = doc.RootElement
                            .GetProperty("responseData")
                            .GetProperty("translatedText")
                            .GetString();

                        isSuccess = true; // SÓLO ACÁ ES ÉXITO REAL
                        return new TranslationResultDto { TranslatedText = translatedText };
                    }
                    else
                    {
                        // Capturamos el error real de la API (como el de ZZ-ZZ)
                        errorMessage = doc.RootElement.GetProperty("responseDetails").GetString();
                    }
                }
                else
                {
                    errorMessage = $"Error HTTP: {response.StatusCode}";
                }

                // Si llegamos acá, falló la API o el HTTP
                Logger.LogWarning($"⚠️ MyMemory API falló: {errorMessage}");
                return new TranslationResultDto { TranslatedText = "Error en la traducción" };
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                Logger.LogError(ex, $"❌ Excepción en TranslationService: {ex.Message}");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                // Ahora la métrica reflejará la realidad gracias al isSuccess = true de arriba
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