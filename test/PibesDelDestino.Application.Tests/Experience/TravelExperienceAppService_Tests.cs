using PibesDelDestino.Destinations;
using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.Experiences
{
    // Clase Abstracta con la lógica de pruebas
    public abstract class TravelExperienceAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly ITravelExperienceAppService _experienceService;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;
        private readonly IRepository<Destination, Guid> _destinationRepository;

        protected TravelExperienceAppService_Tests()
        {
            _experienceService = GetRequiredService<ITravelExperienceAppService>();
            _experienceRepository = GetRequiredService<IRepository<TravelExperience, Guid>>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
        }

        [Fact]
        public async Task CreateAsync_Should_Create_Valid_Experience()
        {
            // 1. Arrange: Necesitamos un destino real
            var dest = new Destination(Guid.NewGuid(), "Roma", "Italia", "Roma", 5000, "img", DateTime.Now, new Coordinates(0, 0));
            await _destinationRepository.InsertAsync(dest, autoSave: true);

            var input = new CreateUpdateTravelExperienceDto
            {
                DestinationId = dest.Id,
                Title = "Viaje soñado",
                Description = "Increíble todo",
                Rating = 5,
                Date = DateTime.Now
            };

            // 2. Act
            var result = await _experienceService.CreateAsync(input);

            // 3. Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Title.ShouldBe("Viaje soñado");

            // Verificamos en la base de datos
            var dbEntity = await _experienceRepository.GetAsync(result.Id);
            dbEntity.Rating.ShouldBe(5);
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_If_Owner()
        {
            // 1. Arrange
            var dest = new Destination(Guid.NewGuid(), "Paris", "Francia", "Paris", 5000, "img", DateTime.Now, new Coordinates(0, 0));
            await _destinationRepository.InsertAsync(dest, autoSave: true);

            // Creamos una experiencia inicial (Simulando que la creé YO)
            // Nota: En los tests, CurrentUser.Id suele ser null o un valor fijo si no se configura.
            // Para que esto funcione sin login complejo, asumimos que al insertar directo
            // el CreatorId se setea (o lo forzamos si es necesario, pero probemos así primero).

            var inputCreate = new CreateUpdateTravelExperienceDto
            {
                DestinationId = dest.Id,
                Title = "Original",
                Description = "Desc",
                Rating = 3,
                Date = DateTime.Now
            };
            var createdDto = await _experienceService.CreateAsync(inputCreate);

            // 2. Act: Modificamos
            var updateInput = new CreateUpdateTravelExperienceDto
            {
                DestinationId = dest.Id,
                Title = "Editado",
                Description = "Nueva descripción",
                Rating = 4,
                Date = DateTime.Now
            };

            var result = await _experienceService.UpdateAsync(createdDto.Id, updateInput);

            // 3. Assert
            result.Title.ShouldBe("Editado");
            result.Rating.ShouldBe(4);
        }

        [Fact]
        public async Task DeleteAsync_Should_Fail_If_Not_Owner()
        {
            // 1. Arrange
            var dest = new Destination(Guid.NewGuid(), "Madrid", "España", "Madrid", 5000, "img", DateTime.Now, new Coordinates(0, 0));
            await _destinationRepository.InsertAsync(dest, autoSave: true);

            // Insertamos una experiencia "a mano" forzando un Usuario DIFERENTE
            var otherUserId = Guid.NewGuid();
            var exp = new TravelExperience(Guid.NewGuid(), otherUserId, dest.Id, "Ajeno", "No tocar", DateTime.Now, 3);

            await _experienceRepository.InsertAsync(exp, autoSave: true);

            // 2. Act & Assert
            // Intentamos borrarla con el usuario actual (que no es otherUserId)
            await Assert.ThrowsAsync<AbpAuthorizationException>(async () =>
            {
                await _experienceService.DeleteAsync(exp.Id);
            });
        }
    }
}
//✔️ Llama al repositorio: CreateAsync guarda en la DB real.

//✔️ Valida reglas: Si intentás borrar algo ajeno, falla (AbpAuthorizationException).

//✔️ Transforma DTOs: Verificamos que lo que vuelve (result) tenga los datos correctos.