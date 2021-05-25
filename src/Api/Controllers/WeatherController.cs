using System.Threading.Tasks;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController:ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }
        [HttpGet]

        public async Task<ActionResult> GetCityOfWeather(string city)
        {
            var result = await _weatherService.GetWeatherOfCity(city);
            return Ok(result);
        }
    }
}