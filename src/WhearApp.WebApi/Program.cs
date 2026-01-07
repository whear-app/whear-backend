using Scalar.AspNetCore;
using WhearApp.Application.Identity.Services;
using WhearApp.Infrastructure.Identity.Services;
using WhearApp.WebApi.Endpoints;
using WhearApp.WebApi.Extensions.DI;
using WhearApp.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOpenApi();
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();
app.UseGlobalExceptionHandler();
app.UseStatusCodePages();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "WhearApp API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseHttpsRedirection();

app.MapGroup("/api/v1")
    .WithOpenApi()
    .MapV1Endpoints();

app.Run();

