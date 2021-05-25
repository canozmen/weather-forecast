using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Serilog.Core;

namespace Api
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private IConfiguration Configuration;
        private readonly Logger _logger;
        public Startup(IHostEnvironment env)
        {
            var basePath = PlatformServices.Default.Application.ApplicationBasePath;
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = configurationBuilder.Build();

            _logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo.Console()
                   .CreateLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
              .AddJsonOptions(o =>
              {
                  o.JsonSerializerOptions
                    .ReferenceHandler = ReferenceHandler.Preserve;
                    
            });
            services.AddControllers();
            services.AddTransient<IWeatherService, WeatherService>();
            services.AddOptions();
            services.Configure<WeatherApiConfiguration>(Configuration.GetSection("WeatherApiConfiguration"));
            services.AddLogging(lg => lg.AddSerilog(_logger));
            services.AddHttpClient();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // app.UseSwagger();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });


            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
