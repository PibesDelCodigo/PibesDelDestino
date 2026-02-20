using PibesDelDestino.EntityFrameworkCore;
using Xunit;

namespace PibesDelDestino.GeoDb
{
    // Esta es la clase que va a ejecutar xUnit usando la base de datos real
    public class EfCoreGeoDbCitySearchService_Tests : GeoDbCitySearchService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {
    }
}