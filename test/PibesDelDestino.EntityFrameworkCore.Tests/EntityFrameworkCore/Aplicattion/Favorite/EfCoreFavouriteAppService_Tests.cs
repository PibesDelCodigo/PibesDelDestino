using PibesDelDestino.EntityFrameworkCore;
using Xunit;

namespace PibesDelDestino.Favorites
{
    public class EfCoreFavoriteAppService_Tests : FavoriteAppService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {
        // Hereda todo y usa la DB real.
    }
}