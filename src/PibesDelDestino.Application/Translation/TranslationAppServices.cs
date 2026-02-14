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

        // Este método se encarga de traducir un texto utilizando la API de MyMemory.
        // Mide el tiempo de respuesta, maneja errores y registra métricas.
        public async Task<TranslationResultDto> TranslateAsync(TranslateDto input)
        {
            var stopwatch = Stopwatch.StartNew();
            var isSuccess = false;
            var errorMessage = "";

            //Aca se hace la llamada a la API de MyMemory para traducir el texto.
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input.TextToTranslate)}&langpair=es|{input.TargetLanguage}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {

                    // Se lee la respuesta JSON y se extrae el texto traducido.
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

                await _metricRepository.InsertAsync(new ApiMetric(
                    GuidGenerator.Create(),
                    serviceName: "MyMemoryTranslationApi",
                    endpoint: "/get",
                    isSuccess: isSuccess,
                    responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                    errorMessage: isSuccess ? string.Empty : errorMessage
                ));
            }
        }
    }
}