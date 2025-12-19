using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PibesDelDestino.Metrics;
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
            var errorMessage = ""; // Inicializamos vacío

            try
            {
                var client = _httpClientFactory.CreateClient();
                // Usamos MyMemory API
                var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input.TextToTranslate)}&langpair=es|{input.TargetLanguage}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(jsonString);
                    var translatedText = doc.RootElement
                                            .GetProperty("responseData")
                                            .GetProperty("translatedText")
                                            .GetString();

                    isSuccess = true;
                    stopwatch.Stop();

                    return new TranslationResultDto { TranslatedText = translatedText };
                }
                else
                {
                    isSuccess = false;
                    errorMessage = $"Error HTTP: {response.StatusCode}";
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                stopwatch.Stop();
                throw;
            }
            finally
            {
                // 👇 ACÁ ESTABA EL ERROR. 
                // Antes decíamos: isSuccess ? null : errorMessage.
                // Ahora usamos string.Empty para que la BD no se queje.

                await _metricRepository.InsertAsync(new ApiMetric(
                    GuidGenerator.Create(),
                    serviceName: "MyMemoryTranslationApi",
                    endpoint: "/get",
                    isSuccess: isSuccess,
                    responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                    errorMessage: isSuccess ? string.Empty : errorMessage // ✅ CAMBIO CLAVE
                ));
            }
        }
    }
}