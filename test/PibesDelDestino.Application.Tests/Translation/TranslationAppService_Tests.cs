using NSubstitute;
using Shouldly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;
using System.Linq;
using PibesDelDestino.Metrics;

namespace PibesDelDestino.Translation
{
    public abstract class TranslationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly ITranslationAppService _translationService;
        protected readonly IRepository<ApiMetric, Guid> _metricRepository;

        protected TranslationAppService_Tests()
        {
            _translationService = GetRequiredService<ITranslationAppService>();
            _metricRepository = GetRequiredService<IRepository<ApiMetric, Guid>>();
        }

        [Fact]
        public async Task TranslateAsync_Should_Return_Translated_Text_And_Log_Metric()
        {
            // Act
            var input = new TranslateDto { TextToTranslate = "Hola", TargetLanguage = "en" };
            var result = await _translationService.TranslateAsync(input);

            // Assert
            result.ShouldNotBeNull();
            result.TranslatedText.ShouldNotBeNullOrEmpty();

            // Verificamos métricas en la DB (Test de integración)
            var metrics = await _metricRepository.GetListAsync();
            metrics.ShouldContain(x => x.ServiceName == "MyMemoryTranslationApi" && x.IsSuccess);
        }

        [Fact]
        public async Task TranslateAsync_Should_Handle_Network_Error_With_NSubstitute()
        {
            // Arrange
            // Creamos el sustituto para el factory
            var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();

            // Programamos que al pedir un cliente, este lance una excepción de red
            mockHttpClientFactory.CreateClient(Arg.Any<string>())
                .Returns(x => throw new HttpRequestException("Network failure"));

            var mockMetricRepo = Substitute.For<IRepository<ApiMetric, Guid>>();

            // Construimos el servicio manualmente para inyectar los Mocks
            var service = new TranslationAppService(
                mockHttpClientFactory,
                mockMetricRepo
            );

            // Act & Assert
            // Como tu código re-lanza la excepción en el catch, esperamos el throw
            await Should.ThrowAsync<HttpRequestException>(async () =>
                await service.TranslateAsync(new TranslateDto { TextToTranslate = "Prueba" })
            );

            // Verificación con NSubstitute: ¿Se recibió el intento de guardar la métrica de error?
            await mockMetricRepo.Received(1).InsertAsync(
                Arg.Is<ApiMetric>(m => m.IsSuccess == false),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            );
        }
    }
}