using Blazored.LocalStorage;
using Blazored.Modal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SynopsisClient.Model;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

// Set version number for the assembly.
[assembly: AssemblyVersion("1.0.*")]

namespace SynopsisClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Logging.AddConfiguration(
                builder.Configuration.GetSection("Logging"));

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<SynopsisHandler>();
            builder.Services.AddScoped<UserManager>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddBlazoredModal();

            builder.Logging
                .ClearProviders()
                .AddProvider(new SynopsisClientLoggerProvider(new SynopsisClientLoggerConfiguration
                {
                    MinimumLogLevel = LogLevel.Trace
                }));

            await builder.Build().RunAsync();
        }
    }
}