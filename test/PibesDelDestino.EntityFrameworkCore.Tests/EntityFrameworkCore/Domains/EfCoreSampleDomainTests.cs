using PibesDelDestino.Samples;
using Xunit;

namespace PibesDelDestino.EntityFrameworkCore.Domains;

[Collection(PibesDelDestinoTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PibesDelDestinoEntityFrameworkCoreTestModule>
{

}
