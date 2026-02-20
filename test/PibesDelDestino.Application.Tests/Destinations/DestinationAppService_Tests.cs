using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Experiences;
using PibesDelDestino.Notifications;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace PibesDelDestino.Destinations
{
    public abstract class DestinationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IDestinationAppService _destinationService;
        private readonly IRepository<Destination, Guid> _destinationRepository;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;
        private INotificationManager _notificationManagerMock;

        public DestinationAppService_Tests()
        {
            _destinationService = GetRequiredService<IDestinationAppService>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
            _experienceRepository = GetRequiredService<IRepository<TravelExperience, Guid>>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            _notificationManagerMock = Substitute.For<INotificationManager>();
            services.AddSingleton(_notificationManagerMock);
        }

        [Fact]
        public async Task Should_Get_List_Of_Destinations()
        {
            // Arrange
            var destination = new Destination(Guid.NewGuid(), "Destino", "País", "Ciudad", 1000, "foto.jpg", DateTime.Now, new Coordinates(10, 20));
            await _destinationRepository.InsertAsync(destination, autoSave: true);

            // Act
            var result = await _destinationService.GetListAsync(new PagedAndSortedResultRequestDto());

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldContain(d => d.Id == destination.Id);
            result.ShouldNotBeNull();
            result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);

            var destinationDto = result.Items.FirstOrDefault(d => d.Id == destination.Id);
            destinationDto.ShouldNotBeNull();

            // Verificar todas las propiedades mapeadas
            destinationDto.Name.ShouldBe(destination.Name);
            destinationDto.Country.ShouldBe(destination.Country);
            destinationDto.City.ShouldBe(destination.City);
            destinationDto.Population.ShouldBe(destination.Population);
            destinationDto.Photo.ShouldBe(destination.Photo);
            destinationDto.UpdateDate.ShouldBe(destination.UpdateDate); // Considera usar `ShouldBeInRange` si hay diferencias de ticks
            destinationDto.Coordinates.ShouldNotBeNull();
            destinationDto.Coordinates.Latitude.ShouldBe(destination.Coordinates.Latitude);
            destinationDto.Coordinates.Longitude.ShouldBe(destination.Coordinates.Longitude);
            destinationDto.AverageRating.ShouldBe(0);
        }

        [Fact]
        public async Task Should_Get_Destination_With_Average_Rating()
        {
            // Arrange
            var destination = new Destination(Guid.NewGuid(), "Destino", "País", "Ciudad", 1000, "foto.jpg", DateTime.Now, new Coordinates(10, 20));
            await _destinationRepository.InsertAsync(destination, autoSave: true);

            var experiences = new List<TravelExperience>
            {
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination.Id, "Exp1", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination.Id, "Exp2", "Desc", DateTime.Now, 3)
            };

            foreach (var exp in experiences)
            {
                await _experienceRepository.InsertAsync(exp, autoSave: true);
            }

            // Act
            var dto = await _destinationService.GetAsync(destination.Id);

            // Assert
            // (5+3)/2
            dto.AverageRating.ShouldBe(4.0);
        }

        [Fact]
        public async Task Should_Create_A_Valid_Destinations()
        {
            //Arrange
            var input = new CreateUpdateDestinationDto
            {
                Name = "Destino",
                Country = "País",
                City = "Ciudad",
                Population = 1000,
                Photo = "foto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 10, Longitude = 20 }
            };

            // Act
            var result = await _destinationService.CreateAsync(input);

            //Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Name.ShouldBe(input.Name);
            result.Country.ShouldBe(input.Country);
            result.City.ShouldBe(input.City);
            result.Population.ShouldBe(input.Population);
            result.Photo.ShouldBe(input.Photo);
            result.UpdateDate.ShouldBeLessThanOrEqualTo(input.UpdateDate);
            result.Coordinates.Latitude.ShouldBe(input.Coordinates.Latitude);
            result.Coordinates.Longitude.ShouldBe(input.Coordinates.Longitude);
            result.AverageRating.ShouldBe(0);

            var savedDestination = await _destinationRepository.GetAsync(result.Id);
            savedDestination.ShouldNotBeNull();
            savedDestination.Name.ShouldBe(input.Name);
            savedDestination.Population.ShouldBe(input.Population);
            savedDestination.Coordinates.Latitude.ShouldBe(input.Coordinates.Latitude);
        }

        [Fact]
        public async Task Should_Not_Create_Destination_With_Invalid_Values()
        {
            // Arrange
            var input = new CreateUpdateDestinationDto
            {
                Name = "", // Inválido
                Country = "País",
                City = "Ciudad",
                Population = 1000,
                Photo = "foto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 100 /*Invalido*/, Longitude = 20 }
            };

            // Act
            // se ejecuta el método por lambda async y se espera que lance una excepción de validación
            var exception = await Assert.ThrowsAsync<AbpValidationException>(async () =>
                await _destinationService.CreateAsync(input)
            );

            // Assert
            exception.ValidationErrors.ShouldContain(e => e.MemberNames.Contains(nameof(CreateUpdateDestinationDto.Name)));
            exception.ValidationErrors.ShouldContain(e => e.MemberNames.Contains(nameof(CreateUpdateDestinationDto.Coordinates.Latitude)));
        }

        [Fact]
        public async Task Should_Not_Update_Destination_With_Invalid_Data()
        {
            // Arrange
            var createInput = new CreateUpdateDestinationDto
            {
                Name = "Destino Original",
                Country = "País",
                City = "Ciudad",
                Population = 1000,
                Photo = "foto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 10, Longitude = 20 }
            };

            var created = await _destinationService.CreateAsync(createInput);

            // Update
            var updateInput = new CreateUpdateDestinationDto
            {
                Name = "", // Inválido
                Country = "País",
                City = "Ciudad",
                Population = 1000,
                Photo = "foto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 10, Longitude = 20 }
            };

            // Act
            var exception = await Assert.ThrowsAsync<AbpValidationException>(async () =>
                await _destinationService.UpdateAsync(created.Id, updateInput)
            );

            // Assert
            exception.ValidationErrors.ShouldContain(e => e.MemberNames.Contains(nameof(CreateUpdateDestinationDto.Name)));
        }

        [Fact]
        public async Task Should_Update_Destination_Successfully()
        {
            // Arrange
            var createInput = new CreateUpdateDestinationDto
            {
                Name = "Destino Original",
                Country = "País",
                City = "Ciudad",
                Population = 1000,
                Photo = "foto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 10, Longitude = 20 }
            };
            var created = await _destinationService.CreateAsync(createInput);

            var updateInput = new CreateUpdateDestinationDto
            {
                Name = "Destino Actualizado",
                Country = "Nuevo País",
                City = "Nueva Ciudad",
                Population = 2000,
                Photo = "nuevaFoto.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = 30, Longitude = 40 }
            };


            // Act
            var updated = await _destinationService.UpdateAsync(created.Id, updateInput);

            // Assert
            updated.Name.ShouldBe(updateInput.Name);
            updated.Country.ShouldBe(updateInput.Country);
            updated.City.ShouldBe(updateInput.City);
            updated.Population.ShouldBe(updateInput.Population);
            updated.Photo.ShouldBe(updateInput.Photo);
            updated.Coordinates.Latitude.ShouldBe(updateInput.Coordinates.Latitude);
            updated.Coordinates.Longitude.ShouldBe(updateInput.Coordinates.Longitude);

            var updateDb = await _destinationRepository.GetAsync(created.Id);
            updateDb.Name.ShouldBe(updateInput.Name);
            updateDb.Country.ShouldBe(updateInput.Country);
            updateDb.City.ShouldBe(updateInput.City);
            updateDb.Population.ShouldBe(updateInput.Population);
            updateDb.Photo.ShouldBe(updateInput.Photo);
            updateDb.Coordinates.Latitude.ShouldBe(updateInput.Coordinates.Latitude);
            updateDb.Coordinates.Longitude.ShouldBe(updateInput.Coordinates.Longitude);

            await _notificationManagerMock.Received(1).NotifyDestinationUpdateAsync(Arg.Any<Destination>(), Arg.Any<string>());
        }

        [Fact]
        public async Task GetTopDestinationsAsync_Should_Return_Top10_OrderedByAverageRating()
        {
            // Arrange
            var destination1 = new Destination(Guid.NewGuid(), "Destino A", "País A", "Ciudad A", 1000, "fotoA.jpg", DateTime.Now, new Coordinates(10, 20));
            var destination2 = new Destination(Guid.NewGuid(), "Destino B", "País B", "Ciudad B", 2000, "fotoB.jpg", DateTime.Now, new Coordinates(30, 40));
            var destination3 = new Destination(Guid.NewGuid(), "Destino C", "País C", "Ciudad C", 3000, "fotoC.jpg", DateTime.Now, new Coordinates(50, 60));
            var destination4 = new Destination(Guid.NewGuid(), "Destino D", "País D", "Ciudad D", 4000, "fotoD.jpg", DateTime.Now, new Coordinates(70, 80));

            await _destinationRepository.InsertManyAsync([destination1, destination2, destination3, destination4], autoSave: true);

            var experiences = new List<TravelExperience>
            {
                // Destino A: ratings 5 y 4 -> Promedio 4.5
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp1", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp2", "Desc", DateTime.Now, 4),

                // Destino B: 3 -> 3.0
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination2.Id, "Exp3", "Desc", DateTime.Now, 3),

                // Destino C: 5, 5, 5 -> 5.0
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp4", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp5", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp6", "Desc", DateTime.Now, 5),
                // Destino D: nada
            };

            await _experienceRepository.InsertManyAsync(experiences, autoSave: true);

            // 2. Act
            var result = await _destinationService.GetTopDestinationsAsync();

            // Assert
            result.ShouldNotBeNull();

            // Destinano con experiences
            result.Count.ShouldBe(3);

            // 1ro: Destino C
            result[0].Id.ShouldBe(destination3.Id);
            result[0].AverageRating.ShouldBe(5.0);

            // 2do: Destino A
            result[1].Id.ShouldBe(destination1.Id);
            result[1].AverageRating.ShouldBe(4.5);

            // 3ro: Destino B
            result[2].Id.ShouldBe(destination2.Id);
            result[2].AverageRating.ShouldBe(3.0);

            // Destino D excluido
            result.Any(d => d.Id == destination4.Id).ShouldBeFalse();
        }
    }
}