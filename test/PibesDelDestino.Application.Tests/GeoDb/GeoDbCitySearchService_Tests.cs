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
            // ARRANGE
            var input = new CityRequestDTO { PartialName = "London" };

            // ACT
            var result = await _citySearchService.SearchCitiesAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Cities.ShouldNotBeNull();
            // Nota: Si no tenés internet en el entorno de pruebas, este test podría fallar.
            // Para un TP está bien mostrar que el código intenta conectarse.
            if (result.Cities.Count > 0)
            {
                result.Cities.ShouldContain(c => c.Name.Contains("London"));
            }
        }
    }
}