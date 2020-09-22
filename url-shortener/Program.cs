using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.WebUtilities;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILiteDatabase, LiteDatabase>((sp) =>
            {
                var db = new LiteDatabase("shortener.db");
                var collection = db.GetCollection<ShortLink>(BsonAutoId.Int32);
                collection.EnsureIndex(p => p.Url);
                collection.Upsert(new ShortLink
                {
                    Id = 100_000,
                    Url = "https://www.google.com/",
                });
                return db;
            });
            services.AddRouting();
        })
        .Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints((endpoints) =>
            {
                endpoints.MapPost("/shorten", HandleShortenUrl);
                endpoints.MapFallback(HandleUrl);
            });
        });
    })
    .Build();

await host.RunAsync();

static Task WriteResponse(HttpContext context, int status, string response)
{
    context.Response.StatusCode = status;
    return context.Response.WriteAsync(response);
}

static Task HandleShortenUrl(HttpContext context)
{
    // Retrieve our dependencies
    var db = context.RequestServices.GetService<ILiteDatabase>();
    var collection = db.GetCollection<ShortLink>(nameof(ShortLink));

    // Perform basic form validation
    if (!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url"))
    {
        return WriteResponse(context, StatusCodes.Status400BadRequest, "Cannot process request.");
    }
    else
    {
        context.Request.Form.TryGetValue("url", out var formData);
        var requestedUrl = formData.ToString();

        // Test our URL
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
        {
            return WriteResponse(context, StatusCodes.Status400BadRequest, "Could not understand URL");
        }

        var url = result.ToString();
        var entry = collection.Find(p => p.Url == url).FirstOrDefault();

        if (entry is null)
        {
            entry = new ShortLink
            {
                Url = url
            };
            collection.Insert(entry);
        }

        var urlChunk = entry.GetUrlChunk();
        var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
        context.Response.Redirect($"/#{responseUri}");
        return Task.CompletedTask;
    }
}

static Task HandleUrl(HttpContext context)
{
    if (context.Request.Path == "/")
    {
        return context.Response.SendFileAsync("wwwroot/index.htm");
    }

    // Default to home page if no matching url.
    var redirect = "/";

    var db = context.RequestServices.GetService<ILiteDatabase>();
    var collection = db.GetCollection<ShortLink>();

    var path = context.Request.Path.ToUriComponent().Trim('/');
    var id = ShortLink.GetId(path);
    var entry = collection.Find(p => p.Id == id).SingleOrDefault();

    if(entry is not null)
    {
        redirect = entry.Url;
    }

    context.Response.Redirect(redirect);
    return Task.CompletedTask;
}

// Classes

public class ShortLink
{
    public string GetUrlChunk()
    {
        return WebEncoders.Base64UrlEncode(BitConverter.GetBytes(Id));
    }

    public static int GetId(string urlChunk)
    {
        return BitConverter.ToInt32(WebEncoders.Base64UrlDecode(urlChunk));
    }

    public int Id { get; set; }

    public string Url { get; set; }
}
