using System;
using FluentValidation;
using GPC.Boost.SpendX.Data;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Middleware;
using GPC.Boost.SpendX.Repository;
using GPC.Boost.SpendX.Service;
using GPC.Boost.SpendX.Validation.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register Functions Worker Middlewares
builder.UseMiddleware<GlobalExceptionMiddleware>();

// Configuration
var configuration = builder.Configuration;
string connectionString = Environment.GetEnvironmentVariable("PostgresDb") 
    ?? configuration.GetConnectionString("PostgresDb") 
    ?? "Host=localhost;Port=5432;Database=boost_spendx;Username=postgres;Password=postgres;";

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<SpendXDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(120);
    });
});

// Register Dependencies
builder.Services.AddScoped<ISpendXRepository, SpendXRepository>();
builder.Services.AddScoped<ISpendXService, SpendXService>();

// Register FluentValidation Validators
builder.Services.AddScoped<IValidator<EventOfferRequestDto>, EventOfferRequestDtoValidator>();
builder.Services.AddScoped<IValidator<SpendXOverviewRequestDto>, SpendXOverviewRequestDtoValidator>();
builder.Services.AddScoped<IValidator<DataViewRequestDto>, DataViewRequestDtoValidator>();
builder.Services.AddScoped<IValidator<DataViewResponseDto>, DataViewResponseDtoValidator>();
builder.Services.AddScoped<IValidator<SearchByBrandRequestDto>, SearchByBrandRequestDtoValidator>();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

app.Run();
