using Shouldly;
using System.Threading.Tasks;
using PibesDelDestino.Cities;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.GeoDb
{
    public abstract class GeoDbCitySearchService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly ICitySearchService _citySearchService;

        protected GeoDbCitySearchService_Tests()
        {
            // Obtenemos la implementación REAL del servicio desde el contenedor de DI
            _citySearchService = GetRequiredService<ICitySearchService>();
        }

        [Fact]
        public async Task Should_Get_Real_Cities_From_External_Api()
        {
            // ARRANGE
            var input = new CityRequestDTO { PartialName = "London" };

            // ACT
            var result = await _citySearchService.SearchCitiesAsync(input); // Cambiamos a SearchCitiesAsync

            // ASSERT
            result.ShouldNotBeNull();
            result.Cities.ShouldNotBeNull();
            result.Cities.ShouldNotBeEmpty(); // Verificamos que la API devolvió al menos un resultado
            result.Cities.ShouldContain(c => c.Name.Contains("London")); // Verificamos que uno de los resultados es relevante
        }
    }
}