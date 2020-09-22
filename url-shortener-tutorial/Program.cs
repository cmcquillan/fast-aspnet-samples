using System;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Add the necessary components for HTTP routing.
            services.AddRouting();
        })
        .Configure(app =>
        {
            // Use routing in the pipeline.
            app.UseRouting();

            // Use the endpoint routing module to map URLs to functions.
            app.UseEndpoints((endpoints) =>
            {
                endpoints.MapGet("/", (ctx) =>
                {
                    return ctx.Response.SendFileAsync("index.html");
                });
            });
        });
    })
    .Build();

await host.RunAsync();