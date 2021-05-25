using System.Threading.Tasks;
using Api.Models;

namespace Api.Services
{
    public interface IWeatherService
    {
        Task<Temperatures> GetWeatherOfCity(string cityName);
    }
}