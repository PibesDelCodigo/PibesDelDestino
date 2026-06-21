using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PibesDelDestino.Metrics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.TicketMaster
{
    // Actualizamos el nombre para reflejar que testeamos al "especialista"
    public abstract class TicketMasterEventSearchService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IRepository<ApiMetric, Guid> _apiMetricRepo;
        private readonly IConfiguration _configuration;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<TicketMasterEventSearchService> _logger;

        protected TicketMasterEventSearchService_Tests()
        {
            // Obtenemos las instancias reales de ABP
            _searchHistoryRepo = GetRequiredService<IRepository<SearchHistory, Guid>>();
            _apiMetricRepo = GetRequiredService<IRepository<ApiMetric, Guid>>();
            _configuration = GetRequiredService<IConfiguration>();
            _guidGenerator = GetRequiredService<IGuidGenerator>();

            // Podemos usar un Logger real del framework de pruebas para que quede limpio
            _logger = GetRequiredService<ILogger<TicketMasterEventSearchService>>();
        }

        [Fact]
        public async Task SearchEventsAsync_Should_Return_Events_When_Api_Responds_Ok()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();

            var fakeJson = JsonSerializer.Serialize(new
            {
                _embedded = new
                {
                    events = new[]
                    {
            new
            {
                name = "Lollapalooza",
                url = "https://ticketmaster/lolla",
                images = new[] { new { url = "img.jpg" } },
                dates = new
                {
                    start = new { localDate = "2026-03-20" }
                }
            }
        }
                }
            });

            var mockClient = CreateMockHttpClient(HttpStatusCode.OK, fakeJson);
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            // CREAMOS EL NUEVO SERVICIO ESPECIALIZADO
            var service = new TicketMasterEventSearchService(
                httpClientFactoryMock,
                _configuration,
                _searchHistoryRepo,
                _apiMetricRepo,
                _logger,
                _guidGenerator
            );

            // 2. Act
            var result = await service.SearchEventsAsync("Buenos Aires");

            // 3. Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task SearchEventsAsync_Should_Return_Empty_When_Api_Fails()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Error");
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            var service = new TicketMasterEventSearchService(
                httpClientFactoryMock,
                _configuration,
                _searchHistoryRepo,
                _apiMetricRepo,
                _logger,
                _guidGenerator
            );

            // 2. Act
            var result = await service.SearchEventsAsync("Cordoba");

            // 3. Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task SearchEventsAsync_Should_Handle_Network_Exception_Gracefully()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var failingHandler = new FailingHttpMessageHandler();
            var mockClient = new HttpClient(failingHandler);
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            var service = new TicketMasterEventSearchService(
                httpClientFactoryMock,
                _configuration,
                _searchHistoryRepo,
                _apiMetricRepo,
                _logger,
                _guidGenerator
            );

            // 2. Act
            var result = await service.SearchEventsAsync("Mendoza");

            // 3. Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        // --- HELPERS PARA MOCKEAR EL HTTP (Se mantienen idénticos) ---
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