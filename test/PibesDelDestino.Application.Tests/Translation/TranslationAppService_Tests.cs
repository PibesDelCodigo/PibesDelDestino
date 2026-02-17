using System;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using PibesDelDestino.Translation;
using PibesDelDestino.Metrics;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace PibesDelDestino.Translation
{
    public class TranslationAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly ITranslationAppService _translationAppService;
        private readonly IRepository<ApiMetric, Guid> _metricRepositoryMock;

        public TranslationAppService_Tests()
        {
            //Mock del repositorio de métricas
            _metricRepositoryMock = Substitute.For<IRepository<ApiMetric, Guid>>();

            //Obtenemos el IHttpClientFactory real (para que la traducción funcione de verdad)
            var httpClientFactory = GetRequiredService<IHttpClientFactory>();

            //Instanciamos usando el Proxy público
            _translationAppService = new TranslationAppServiceTestProxy(
                httpClientFactory,
                _metricRepositoryMock,
                ServiceProvider
            );
        }

        [Fact]
        public async Task Should_Translate_Text_Successfully()
        {
            var input = new TranslateDto
            {
                TextToTranslate = "Hello",
                TargetLanguage = "es"
            };

            var result = await _translationAppService.TranslateAsync(input);

            result.ShouldNotBeNull();
            result.TranslatedText.ShouldNotBeNullOrEmpty();

            // Verificamos que se intentó insertar una métrica en el repositorio
            await _metricRepositoryMock.Received(1).InsertAsync(Arg.Any<ApiMetric>());
        }
    }


    public class TranslationAppServiceTestProxy : TranslationAppService
    {
        public TranslationAppServiceTestProxy(
            IHttpClientFactory httpClientFactory,
            IRepository<ApiMetric, Guid> metricRepository,
            IServiceProvider serviceProvider)
            : base(httpClientFactory, metricRepository)
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}