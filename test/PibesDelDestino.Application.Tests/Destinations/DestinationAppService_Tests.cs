using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Experiences;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace PibesDelDestino.Destinations
{
    // Heredamos de ApplicationTestBase, que nos da la Base de Datos en Memoria GRATIS
    public abstract class DestinationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IDestinationAppService _destinationService;
        private readonly IRepository<Destination, Guid> _destinationRepository;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;

        public DestinationAppService_Tests()
        {
            // En lugar de crear Mocks, pedimos los servicios REALES al contenedor de pruebas
            _destinationService = GetRequiredService<IDestinationAppService>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
            _experienceRepository = GetRequiredService<IRepository<TravelExperience, Guid>>();
        }

        [Fact]
        public async Task Should_Get_List_Of_Destinations()
        {
            // Arrange
            var destination = new Destination(Guid.NewGuid(), "Destino Test", "País Test", "Ciudad Test", 1000, "foto.jpg", DateTime.Now, new Coordinates(10, 20));
            await _destinationRepository.InsertAsync(destination, autoSave: true);

            // Act
            var result = await _destinationService.GetListAsync(new PagedAndSortedResultRequestDto());

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldContain(d => d.Id == destination.Id);
        }

        [Fact]
        public async Task Should_Create_A_Valid_Destinations()
        {
            //Arrange-Act
            var result = await _destinationService.CreateAsync(
                new CreateUpdateDestinationDto
                {
                    Name = "Nuevo Destino",
                    Country = "País Nuevo",
                    City = "Ciudad Nueva",
                    Population = 1500,
                    Photo = "nuevaFoto.jpg",
                    UpdateDate = DateTime.Now,
                    Coordinates = new CoordinatesDto { Latitude = 15, Longitude = 25 }
                });

            //Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Name.ShouldBe("Nuevo Destino");
        }

        [Fact]
        public async Task GetTopDestinationsAsync_Should_Return_Top10_OrderedByAverageRating()
        {
            // 1. ARRANGE (Preparar los datos)

            // Creamos los destinos
            var destination1 = new Destination(Guid.NewGuid(), "Destino A", "País A", "Ciudad A", 1000, "fotoA.jpg", DateTime.Now, new Coordinates(10, 20));
            var destination2 = new Destination(Guid.NewGuid(), "Destino B", "País B", "Ciudad B", 2000, "fotoB.jpg", DateTime.Now, new Coordinates(30, 40));
            var destination3 = new Destination(Guid.NewGuid(), "Destino C", "País C", "Ciudad C", 3000, "fotoC.jpg", DateTime.Now, new Coordinates(50, 60));
            var destination4 = new Destination(Guid.NewGuid(), "Destino D", "País D", "Ciudad D", 4000, "fotoD.jpg", DateTime.Now, new Coordinates(70, 80));

            // ¡ACÁ ESTÁ LA CLAVE! 
            // En vez de mockear el repositorio, los guardamos de verdad en la DB de memoria.
            await _destinationRepository.InsertAsync(destination1, autoSave: true);
            await _destinationRepository.InsertAsync(destination2, autoSave: true);
            await _destinationRepository.InsertAsync(destination3, autoSave: true);
            await _destinationRepository.InsertAsync(destination4, autoSave: true);

            // Creamos las experiencias (ratings)
            var experiences = new List<TravelExperience>
            {
                // Destino A: ratings 5 y 4 -> Promedio 4.5
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp1", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp2", "Desc", DateTime.Now, 4),
                // Destino B: rating 3 -> Promedio 3.0
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination2.Id, "Exp3", "Desc", DateTime.Now, 3),
                // Destino C: ratings 5, 5, 5 -> Promedio 5.0
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp4", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp5", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp6", "Desc", DateTime.Now, 5),
                // Destino D: Sin experiencias -> Promedio 0 (o no aparece, según tu lógica)
            };

            // Las guardamos en la DB de memoria
            foreach (var exp in experiences)
            {
                await _experienceRepository.InsertAsync(exp, autoSave: true);
            }

            // 2. ACT (Ejecutar la prueba)
            // Llamamos al servicio real. Él va a ir a la DB de memoria, hacer el cálculo y volver.
            var result = await _destinationService.GetTopDestinationsAsync();

            // 3. ASSERT (Verificar)
            result.ShouldNotBeNull();

            // Destinos con ratings: A, B, C (D no tiene ratings, así que si tu lógica lo excluye, son 3)
            result.Count.ShouldBe(3);

            // Verificamos el Orden (Mayor puntaje primero)

            // 1ro: Destino C (Promedio 5.0)
            result[0].Id.ShouldBe(destination3.Id);
            result[0].AverageRating.ShouldBe(5.0);

            // 2do: Destino A (Promedio 4.5)
            result[1].Id.ShouldBe(destination1.Id);
            result[1].AverageRating.ShouldBe(4.5);

            // 3ro: Destino B (Promedio 3.0)
            result[2].Id.ShouldBe(destination2.Id);
            result[2].AverageRating.ShouldBe(3.0);
        }
    }
}