using Shouldly;
using System;
using Volo.Abp;
using Xunit;

namespace PibesDelDestino.Ratings
{
    public class RatingDomain_Tests
    {
        [Fact]
        public void Should_Create_Rating_With_Valid_Data()
        {
            // Arrange & Act
            var rating = new Rating(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, "Increíble!");

            // Assert
            rating.Score.ShouldBe(5);
            rating.Comment.ShouldBe("Increíble!");
        }

        [Fact]
        public void Should_Throw_Exception_When_Score_Is_Invalid()
        {
            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
            {
                new Rating(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 6, "Mal");
            });

            exception.Code.ShouldBe("PibesDelDestino:Rating:InvalidScore");
        }

        [Fact]
        public void Should_Update_Score_And_Comment_Correctly()
        {
            // Arrange
            var rating = new Rating(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "Meh");

            // Act
            rating.Update(5, "Cambié de opinión, es genial!");

            // Assert
            rating.Score.ShouldBe(5);
            rating.Comment.ShouldBe("Cambié de opinión, es genial!");
        }
    }
}