using PibesDelDestino.Destinations;
using PibesDelDestino.EntityFrameworkCore;
using Xunit;

namespace PibesDelDestino.EntityFrameworkCore.Destinations
{
    // Esta es la clase que XUnit va a encontrar. 
    // Al heredar de la anterior, ejecuta todos sus tests.
    public class EfCoreDestinationAppService_Tests : DestinationAppService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {
        // No hace falta poner código acá, hereda todo de la clase base genérica.
    }
}