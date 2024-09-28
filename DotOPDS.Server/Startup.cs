using DotOPDS.Shared;
using DotOPDS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text.Json.Serialization;
using DotOPDS.DbLayer;
using DotOPDS.Server.DevData;
using DotOPDS.Server.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotOPDS.Server.Services;
using Microsoft.Extensions.Logging;

namespace DotOPDS;

public class Startup(IConfiguration configuration)
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(o =>
        {
            o.AddConsole();
            o.AddSeq("http://localhost:5341", "apikey");
        });

        services
            .AddHttpContextAccessor()
            .AddLocalization()
            .AddControllersWithViews(ConfigureMvcOptions)
            .AddJsonOptions(options =>
               options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        services.AddDbContext<DotOPDSDbContext>(options =>
        {
            // TODO: Replace in-memory store by persistent one like MsSQL of pgSQL
            options.UseInMemoryDatabase(nameof(DotOPDSDbContext));
            // END OF TODO

            // Register the entity sets needed by OpenIddict.
            options.UseOpenIddict();
        });

        services.RegisterAuth();
        // TODO: remove after development and moving from InMemoryDb to persistent storage like MsSQL or pgSQL
        services.AddHostedService<TestData>();
        // END OF TODO
        SharedServices.ConfigureServices(services, configuration);

        services.AddScoped<BookParsersPool>();
        services.AddSingleton<ConverterService>();
        services.AddSingleton<FileUtils>();
        services.AddSingleton<MimeHelper>();
        services.AddScoped<IOwnUserManagerService, OwnUserManagerService>();

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new RequireHttpsAttribute());
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add(HeaderNames.Server, Constants.ServerName);
            await next.Invoke();
        });

        var supportedCultures = new[] { "en", "ru" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        app.UseRequestLocalization(localizationOptions);

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            //endpoints.MapControllers();
            //endpoints.MapFallbackToFile("index.html");
        });
    }

    private void ConfigureMvcOptions(MvcOptions options)
    {
        options.OutputFormatters.Insert(0, new AtomXmlMediaTypeFormatter());
        options.RespectBrowserAcceptHeader = true;
    }
}
