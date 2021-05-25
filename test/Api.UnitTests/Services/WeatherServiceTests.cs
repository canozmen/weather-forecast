using Api.Configurations;
using Api.Services;
using AutoBogus;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Api.UnitTests.Services
{
    public class WeatherServiceTests
    {
        private MockRepository _mockRepository;

        private HttpClient _mockHttpClient;
        private IOptions<WeatherApiConfiguration> _mockOptions;

        public WeatherServiceTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);

            
            var configuration=  AutoFaker.Generate<WeatherApiConfiguration>();
            configuration.BaseUrl = "https://moq.com";
            _mockOptions = new OptionsWrapper<WeatherApiConfiguration>(configuration);
        }

        private WeatherService CreateService()
        {
            return new WeatherService(_mockHttpClient, _mockOptions);
        }

        [Fact]
        public async Task GetWeatherOfCity_ExpectedBehavior()
        {
            // Arrange
            
            var cityName = "moq-city";
            var mockMessageHandler = _mockRepository.Create<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}"),
            };

            mockMessageHandler
                .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);
            _mockHttpClient = new HttpClient(mockMessageHandler.Object);
            var service = CreateService();

            // Act
            var result = await service.GetWeatherOfCity(cityName);

            // Assert
            result
                .Should()
                .NotBeNull();
            result
                .Should()
                .As<Models.Temperatures>();
            
        }
    }
}
