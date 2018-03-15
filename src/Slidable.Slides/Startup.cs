using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slidable.Slides.Options;

namespace Slidable.Slides
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<StorageOptions>(Configuration.GetSection("Storage"));
            services.AddSingleton<IApiKeyProvider, ApiKeyProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<SlidesMiddleware>();
        }
    }
}
