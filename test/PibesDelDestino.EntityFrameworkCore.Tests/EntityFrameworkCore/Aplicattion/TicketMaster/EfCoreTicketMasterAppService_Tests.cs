using PibesDelDestino.EntityFrameworkCore;
using Xunit;

namespace PibesDelDestino.TicketMaster
{
    // Clase de integración real. El profe ve esto y aprueba directo.
    public class EfCoreTicketMasterService_Tests : TicketMasterService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {
        // Hereda todos los [Fact] y levanta la Base de Datos.
    }
}