using System.Threading.Tasks;
using PibesDelDestino.Cities;
using Shouldly;
using Xunit;
using PibesDelDestino;

namespace PibesDelDestino.GeoDb
{
    public class GeoDbCitySearchService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly ICitySearchService _citySearchService;

        public GeoDbCitySearchService_Tests()
        {
            _citySearchService = GetRequiredService<ICitySearchService>();
        }

        [Fact]
        public async Task Should_Get_Real_Cities_From_External_Api()
        {
            var input = new CityRequestDTO { PartialName = "London" };
            var result = await _citySearchService.SearchCitiesAsync(input);

            result.ShouldNotBeNull();
            result.Cities.ShouldNotBeNull();
            if (result.Cities.Count > 0)
            {
                result.Cities.ShouldContain(c => c.Name.Contains("London"));
            }
        }
    }
}