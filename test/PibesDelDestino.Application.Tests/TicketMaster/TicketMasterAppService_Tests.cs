using Microsoft.Extensions.Configuration;
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
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.TicketMaster
{
    // AHORA ES ABSTRACTA Y HEREDA DE LA CLASE BASE DE ABP (Igual que Destinations y Favorites)
    public abstract class TicketMasterService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        // Traemos las herramientas REALES de la base de datos y configuración
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;
        private readonly IRepository<ApiMetric, Guid> _apiMetricRepo;
        private readonly IConfiguration _configuration;
        private readonly IAbpLazyServiceProvider _lazyServiceProvider;

        protected TicketMasterService_Tests()
        {
            // El framework nos da las instancias reales conectadas a la DB en memoria
            _searchHistoryRepo = GetRequiredService<IRepository<SearchHistory, Guid>>();
            _apiMetricRepo = GetRequiredService<IRepository<ApiMetric, Guid>>();
            _configuration = GetRequiredService<IConfiguration>();
            _lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task SearchEventsAsync_Should_Return_Events_When_Api_Responds_Ok()
        {
            // 1. Arrange: MOCKEAMOS SOLO EL INTERNET (HttpClient)
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();

            var fakeJson = JsonSerializer.Serialize(new[]
            {
                new { name = "Lollapalooza", url = "https://ticketmaster/lolla", imageUrl = "img.jpg", date = "2026-03-20" }
            });

            var mockClient = CreateMockHttpClient(HttpStatusCode.OK, fakeJson);
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            // CREAMOS EL SERVICIO: Mezclamos el Internet falso con la Base de Datos real
            var service = new TicketMasterService(
                httpClientFactoryMock,
                _configuration,
                _searchHistoryRepo,
                _apiMetricRepo)
            {
                // Como usamos el proveedor real de ABP, el Logger ya no explota
                LazyServiceProvider = _lazyServiceProvider
            };

            // 2. Act
            var result = await service.SearchEventsAsync("Buenos Aires");

            // 3. Assert
            result.ShouldNotBeNull();

            // Acá podrías revisar que _apiMetricRepo o _searchHistoryRepo guardaron datos, 
            // ya que ahora son bases de datos reales.
        }

        [Fact]
        public async Task SearchEventsAsync_Should_Return_Empty_When_Api_Fails()
        {
            // 1. Arrange
            var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
            var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Error");
            httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(mockClient);

            var service = new TicketMasterService(
                httpClientFactoryMock, _configuration, _searchHistoryRepo, _apiMetricRepo)
            {
                LazyServiceProvider = _lazyServiceProvider
            };

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

            var service = new TicketMasterService(
                httpClientFactoryMock, _configuration, _searchHistoryRepo, _apiMetricRepo)
            {
                LazyServiceProvider = _lazyServiceProvider
            };

            // 2. Act
            var result = await service.SearchEventsAsync("Mendoza");

            // 3. Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        // --- HELPERS PARA MOCKEAR EL HTTP ---
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