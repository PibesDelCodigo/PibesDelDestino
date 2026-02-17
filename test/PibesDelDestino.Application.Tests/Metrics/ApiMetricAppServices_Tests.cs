using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using PibesDelDestino.Metrics;

namespace PibesDelDestino.Metrics
{
    public class ApiMetricAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly IApiMetricAppService _apiMetricAppService;
        private readonly IRepository<ApiMetric, Guid> _metricRepositoryMock;

        public ApiMetricAppService_Tests()
        {
            _metricRepositoryMock = Substitute.For<IRepository<ApiMetric, Guid>>();

            _apiMetricAppService = new ApiMetricAppServiceTestProxy(
                _metricRepositoryMock,
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Get_Metrics_List()
        {
            var metric = new ApiMetric(
                Guid.NewGuid(),
                "TestService",
                "/test",
                true,
                150,
                null
            );

            var fakeList = new List<ApiMetric> { metric }.AsQueryable();
            _metricRepositoryMock.GetQueryableAsync().Returns(Task.FromResult(fakeList));

            var result = await _apiMetricAppService.GetListAsync(new GetApiMetricsInput());

            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
            result.Items[0].ServiceName.ShouldBe("TestService");
        }
    }

    public class ApiMetricAppServiceTestProxy : ApiMetricAppService
    {
        public ApiMetricAppServiceTestProxy(
            IRepository<ApiMetric, Guid> repository,
            IServiceProvider serviceProvider) : base(repository, null)
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}