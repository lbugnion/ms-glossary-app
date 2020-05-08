using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AzureWordsOfTheDay
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Redirect rules

            app.Use(async (context, next) =>
            {
                var url = context.Request.Path.Value;

                // TODO Rewrite /topic/subtopic to /topic-subtopic

                if (url.Contains("/topic/"))
                {
                    var parts = url.Split(new char[]
                    {
                        '/'
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 3)
                    {
                        url = $"/{parts[0]}/{parts[1]}_{parts[2]}";
                        context.Request.Path = url;
                    }
                }

                await next();
            });

            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton<MarkdownHelper>();
        }
    }
}