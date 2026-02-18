
using PibesDelDestino.EntityFrameworkCore; // <--- Para traer el Módulo de EF Core
using Xunit;

namespace PibesDelDestino.Experiences
{
    // CAMBIO CLAVE: Usamos <PibesDelDestinoEntityFrameworkCoreTestModule>
    // Esto es lo que carga la Base de Datos y los Repositorios.
    public class EfCoreTravelExperienceAppService_Tests : TravelExperienceAppService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {
        // Hereda todo, no hay que escribir nada.
    }
}