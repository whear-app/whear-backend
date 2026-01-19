using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using WhearApp.Infrastructure.Caching;
using WhearApp.WebApi.Endpoints;
using WhearApp.WebApi.Endpoints.System;
using WhearApp.WebApi.Extensions.DI;
using WhearApp.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.ConfigureOpenApi();
builder.Services.AddCacheService(options =>
{
    var cacheSection = builder.Configuration.GetSection(CacheOptions.SectionName);
    cacheSection.Bind(options);
});
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddBackgroundJobServices();


builder.Services.AddCors(options =>
{
    // Allow all origins, methods, and headers for development
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin();
        policy.AllowAnyMethod();
        policy.AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
app.UseForwardedHeaders();
app.UseGlobalExceptionHandler();
app.UseStatusCodePages();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.Title = "WhearApp API";
        options.Theme = ScalarTheme.Purple;
        
        // Configure authentication
        options.AddPreferredSecuritySchemes("Bearer")
            .AddHttpAuthentication("Bearer", auth =>
            {
                auth.Token = "";
            });
        var addresses = app.Configuration["ASPNETCORE_URLS"] 
                        ?? app.Configuration["urls"] 
                        ?? "http://localhost:5000";
        var serverUrls = addresses.Split(';');
        foreach (var url in serverUrls)
        {
            options.AddServer(new ScalarServer(url.Trim(), "Local Development"));
        }
        options.AddServer("https://whear-server-72cfd82d57fc.herokuapp.com", "Staging");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!")
    .ExcludeFromDescription()
    .ExcludeFromApiReference();

app.MapGroup("/health")
    .WithTags("System")
    .MapHealthEndpoints();

app.MapGroup("/api/v1")
    .WithOpenApi()
    .MapV1Endpoints();

app.Run();

