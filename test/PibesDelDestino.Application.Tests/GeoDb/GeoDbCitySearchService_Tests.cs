using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PibesDelDestino.Cities; // <--- ACÁ ESTÁN TUS DTOs
using PibesDelDestino.Metrics; // Asumiendo que SearchHistory está acá
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace PibesDelDestino.GeoDb
{
    public class GeoDbCitySearchService_Tests
    {
        // 1. Declaramos los Mocks
        private readonly IHttpClientFactory _httpClientFactoryMock;
        private readonly IConfiguration _configurationMock;
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepoMock;
        private readonly IGuidGenerator _guidGeneratorMock;
        private readonly ILogger<GeoDbCitySearchService> _loggerMock;

        public GeoDbCitySearchService_Tests()
        {
            // Inicializamos los actores falsos
            _httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            _configurationMock = Substitute.For<IConfiguration>();
            _searchHistoryRepoMock = Substitute.For<IRepository<SearchHistory, Guid>>();
            _guidGeneratorMock = Substitute.For<IGuidGenerator>();
            _loggerMock = Substitute.For<ILogger<GeoDbCitySearchService>>();

            // Configuración básica fake
            _configurationMock["GeoDb:ApiKey"].Returns("clave-falsa-123");
            _guidGeneratorMock.Create().Returns(Guid.NewGuid());
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Cities_When_Api_Responds_Ok()
        {
            // 1. Arrange
            // Preparamos el JSON falso que devolvería la API
            // Nota: Usamos propiedad "data" minúscula porque así suele venir en JSON APIs
            var fakeJson = JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new { name = "Córdoba", country = "Argentina", region = "Córdoba", latitude = -31.4, longitude = -64.1, population = 1300000 }
                }
            });

            // Creamos el cliente HTTP mentiroso
            var mockClient = CreateMockHttpClient(HttpStatusCode.OK, fakeJson);
            _httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            // Instanciamos el servicio con los Mocks EXACTOS de tu constructor
            var service = new GeoDbCitySearchService(
                _httpClientFactoryMock,
                _configurationMock,
                _searchHistoryRepoMock,
                _guidGeneratorMock,
                _loggerMock
            );

            // Usamos el DTO correcto: CityRequestDTO (con mayúsculas al final, como en tu código)
            var request = new CityRequestDTO { PartialName = "Cor" };

            // 2. Act
            var result = await service.SearchCitiesAsync(request);

            // 3. Assert
            result.ShouldNotBeNull();
            result.Cities.ShouldNotBeEmpty();
            result.Cities[0].Name.ShouldBe("Córdoba");
            result.Cities[0].Country.ShouldBe("Argentina");

            // Verificamos que intentó guardar en el historial
            await _searchHistoryRepoMock.Received(1).InsertAsync(Arg.Any<SearchHistory>());
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Empty_When_Api_Fails()
        {
            // 1. Arrange: Simulamos error 500
            var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Error en GeoDb");
            _httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            var service = new GeoDbCitySearchService(
                _httpClientFactoryMock, _configurationMock, _searchHistoryRepoMock, _guidGeneratorMock, _loggerMock);

            // 2. Act
            var result = await service.SearchCitiesAsync(new CityRequestDTO { PartialName = "Error" });

            // 3. Assert
            result.Cities.ShouldBeEmpty(); // Tu código devuelve lista vacía si falla
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Handle_Exception_Gracefully()
        {
            // 1. Arrange: Simulamos caída de red (Excepción)
            var failingHandler = new FailingHttpMessageHandler();
            var failingClient = new HttpClient(failingHandler);
            _httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(failingClient);

            var service = new GeoDbCitySearchService(
                _httpClientFactoryMock, _configurationMock, _searchHistoryRepoMock, _guidGeneratorMock, _loggerMock);

            // 2. Act
            var result = await service.SearchCitiesAsync(new CityRequestDTO { PartialName = "Boom" });

            // 3. Assert
            result.Cities.ShouldBeEmpty(); // El try-catch captura y devuelve vacío

            // Verificamos que se haya logueado el error
            // (NSubstitute chequea llamadas a extension methods de forma especial, pero esto es opcional)
        }

        // --- HELPERS (Magia Negra para Mockear HTTP) ---

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