using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.SwaggerUI;
using Tilim.Api.Endpoints;
using Tilim.Application.Workers;
using Tilim.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// PixKeyType round-trips as a readable name ("Email", "Cpf", ...) instead of a raw number.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddScoped<WorkerService>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public-profile", p => p.Expire(TimeSpan.FromSeconds(30)).SetVaryByRouteValue("slug"));
    options.AddPolicy("qr-code", p => p.Expire(TimeSpan.FromMinutes(10)).SetVaryByRouteValue("slug").SetVaryByQuery("amount"));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("create-worker", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 5;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI, reading the same /openapi/v1.json. Dev-only tool for trying
    // the raw endpoints — not the page real users see. /swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Tilim API v1");
        options.DocExpansion(DocExpansion.List);
    });

    app.Services.EnsureDevelopmentDatabaseCreated();
}

// UseDefaultFiles before UseStaticFiles: rewrites "/" to "/index.html".
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseOutputCache();
app.UseRateLimiter();

app.MapWorkerEndpoints();

// Own route because "/w/{slug}" isn't a real file — w.html reads the slug
// from the URL client-side (location.pathname).
app.MapGet("/w/{slug}", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "w.html"));
}).ExcludeFromDescription();

app.Run();

// Exposed for Tilim.Api.Tests' WebApplicationFactory<Program>.
public partial class Program;
