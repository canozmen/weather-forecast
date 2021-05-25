using System.Net.Http;
using System.Threading.Tasks;
using Api.Configurations;
using Microsoft.Extensions.Options;
using Api.Models;

namespace Api.Services
{
    public class WeatherService : IWeatherService
    {
        private HttpClient _httpClient;
        private readonly WeatherApiConfiguration _configuration;

        public WeatherService(HttpClient httpClient, IOptions<WeatherApiConfiguration> configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration.Value;
        }

        public async Task<Temperatures> GetWeatherOfCity(string cityName)
        {
            var requestUri = $"{_configuration.BaseUrl}?access_key={_configuration.ApiKey}&query={cityName}";          
            var httpResponse = await _httpClient.GetStringAsync(requestUri);
            
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<Temperatures>(httpResponse);
            return await Task.FromResult(response);
        }
    }


}