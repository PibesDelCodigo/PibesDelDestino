using NSubstitute;
using Shouldly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;
using PibesDelDestino.Metrics;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection; // IMPORTANTE para IAbpLazyServiceProvider

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
            var input = new TranslateDto { TextToTranslate = "Hola", TargetLanguage = "en" };
            var result = await _translationService.TranslateAsync(input);

            result.ShouldNotBeNull();
            var metrics = await _metricRepository.GetListAsync();
            metrics.ShouldContain(x => x.ServiceName == "MyMemoryTranslationApi" && x.IsSuccess);
        }

        [Fact]
        public async Task TranslateAsync_Should_Handle_Network_Error_With_NSubstitute()
        {
            // Arrange
            var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
            mockHttpClientFactory.CreateClient(Arg.Any<string>())
                .Returns(x => throw new HttpRequestException("Network failure"));

            var mockMetricRepo = Substitute.For<IRepository<ApiMetric, Guid>>();

            // 1. Construimos el servicio con el 'new' manual
            var service = new TranslationAppService(mockHttpClientFactory, mockMetricRepo);

            // 2. LA LLAVE MAESTRA:
            // Al asignar el LazyServiceProvider real del entorno de test, 
            // 'GuidGenerator' y 'Logger' dejan de ser nulos automáticamente.
            service.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();

            // Act & Assert
            await Should.ThrowAsync<HttpRequestException>(async () =>
                await service.TranslateAsync(new TranslateDto { TextToTranslate = "Prueba", TargetLanguage = "en" })
            );

            // Verificación del Mock
            await mockMetricRepo.Received(1).InsertAsync(
                Arg.Is<ApiMetric>(m => m.IsSuccess == false && m.ErrorMessage.Contains("Network failure")),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            );
        }
    }
}