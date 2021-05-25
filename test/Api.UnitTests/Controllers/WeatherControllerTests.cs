using Api.Controllers;
using Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Api.UnitTests.Controllers
{
    public class WeatherControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IWeatherService> _mockWeatherService;

        public WeatherControllerTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);

            _mockWeatherService = _mockRepository.Create<IWeatherService>();
        }

        private WeatherController CreateWeatherController()
        {
            return new WeatherController(_mockWeatherService.Object);
        }

        [Fact]
        public async Task GetCityOfWeather_ExpectedBehavior()
        {
            // Arrange
            var weatherController = CreateWeatherController();
            string city = "moq-city";
            Models.Temperatures temperatures = AutoBogus.AutoFaker.Generate<Models.Temperatures>();
            _mockWeatherService
                .Setup(st => st.GetWeatherOfCity(It.IsAny<string>()))
                .ReturnsAsync(temperatures);
            // Act
            var result = await weatherController.GetCityOfWeather(city);

            // Assert
            result
                .Should()
                .NotBeNull()
                .As<OkObjectResult>();
 
        }
    }
}
