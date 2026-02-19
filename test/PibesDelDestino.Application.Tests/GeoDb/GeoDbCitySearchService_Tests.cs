using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PibesDelDestino.Cities;
using PibesDelDestino.Metrics;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.GeoDb
{
    // AHORA ES ABSTRACTA Y HEREDA DE ABP
    public abstract class GeoDbCitySearchService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        // Traemos las herramientas REALES de la base de datos de pruebas
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<GeoDbCitySearchService> _logger;

        protected GeoDbCitySearchService_Tests()
        {
            // El framework nos da las instancias reales conectadas a la DB en memoria
            _searchHistoryRepo = GetRequiredService<IRepository<SearchHistory, Guid>>();
            _guidGenerator = GetRequiredService<IGuidGenerator>();
            _logger = GetRequiredService<ILogger<GeoDbCitySearchService>>();
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Cities_When_Api_Responds_Ok()
        {
            // 1. Arrange: MOCKEAMOS EL HTTP Y LA CONFIGURACIÓN
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var configurationMock = Substitute.For<IConfiguration>();
            configurationMock["GeoDb:ApiKey"].Returns("clave-falsa-123");

            var fakeJson = JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new { name = "Córdoba", country = "Argentina", region = "Córdoba", latitude = -31.4, longitude = -64.1, population = 1300000 }
                }
            });

            var mockClient = CreateMockHttpClient(HttpStatusCode.OK, fakeJson);
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            // CREAMOS EL SERVICIO HÍBRIDO: Api y config falsas + Base de Datos real
            var service = new GeoDbCitySearchService(
                httpClientFactoryMock,
                configurationMock,
                _searchHistoryRepo,
                _guidGenerator,
                _logger
            );

            var request = new CityRequestDTO { PartialName = "Cor" };

            // 2. Act
            var result = await service.SearchCitiesAsync(request);

            // 3. Assert
            result.ShouldNotBeNull();
            result.Cities.ShouldNotBeEmpty();
            result.Cities[0].Name.ShouldBe("Córdoba");
            result.Cities[0].Country.ShouldBe("Argentina");

            // 🔴 EL CAMBIO MAGISTRAL: Ahora verificamos si se guardó en la DB Real
            await WithUnitOfWorkAsync(async () =>
            {
                var historial = await _searchHistoryRepo.GetListAsync();
                historial.Count.ShouldBeGreaterThan(0); // Si es mayor a 0, ¡guardó el historial!
            });
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Empty_When_Api_Fails()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var configurationMock = Substitute.For<IConfiguration>();

            var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Error en GeoDb");
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            var service = new GeoDbCitySearchService(
                httpClientFactoryMock, configurationMock, _searchHistoryRepo, _guidGenerator, _logger);

            // 2. Act
            var result = await service.SearchCitiesAsync(new CityRequestDTO { PartialName = "Error" });

            // 3. Assert
            result.Cities.ShouldBeEmpty();
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Handle_Exception_Gracefully()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var configurationMock = Substitute.For<IConfiguration>();

            var failingHandler = new FailingHttpMessageHandler();
            var failingClient = new HttpClient(failingHandler);
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(failingClient);

            var service = new GeoDbCitySearchService(
                httpClientFactoryMock, configurationMock, _searchHistoryRepo, _guidGenerator, _logger);

            // 2. Act
            var result = await service.SearchCitiesAsync(new CityRequestDTO { PartialName = "Boom" });

            // 3. Assert
            result.Cities.ShouldBeEmpty();
        }

        // --- HELPERS PARA HTTP ---
        private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string jsonContent)
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonContent)
            });
            return new HttpClient(handler);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }

        private class FailingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException("Error de red simulado");
        }
    }
}