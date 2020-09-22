using System;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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
                // Serve root index.html
                endpoints.MapGet("/", (ctx) =>
                {
                    return ctx.Response.SendFileAsync("index.html");
                });

                endpoints.MapPost("/shorten", HandleShortenUrl);
            });
        });
    })
    .Build();

await host.RunAsync();

// Endpoint Methods

static Task HandleShortenUrl(HttpContext context)
{
    // Perform basic form validation
    if (!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Cannot process request");
    }

    context.Request.Form.TryGetValue("url", out var formData);
    var requestedUrl = formData.ToString();

    // Test our URL
    if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Could not understand URL.");
    }

    var url = result.ToString();

    // Temporary short link 
    var entry = new ShortLink
    {
        Id = 123_456_789,
        Url = url
    };

    var urlChunk = entry.GetUrlChunk();
    var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
    return context.Response.WriteAsync(responseUri);
}