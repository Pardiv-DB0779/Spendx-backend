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
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureFunctionsWebApplication(workerApp =>
    {
        workerApp.UseMiddleware<GlobalExceptionMiddleware>();
    })
    .ConfigureOpenApi()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var swaggerUiName = Environment.GetEnvironmentVariable("Swagger:UiName") ?? configuration["Swagger:UiName"];

        services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
        {
            return new OpenApiConfigurationOptions
            {
                Info = new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = !string.IsNullOrEmpty(swaggerUiName) ? swaggerUiName : "GPC Boost SpendX API",
                    Description = "SpendX Offer Type (Spend X Get Y% Off & Loyal) APIs"
                },
                Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                OpenApiVersion = OpenApiVersionType.V3,
                IncludeRequestingHostName = true,
                ForceHttps = false,
                ForceHttp = false
            };
        });

        string connectionString = Environment.GetEnvironmentVariable("PostgresDb")
            ?? configuration.GetConnectionString("PostgresDb")
            ?? configuration["PostgresDb"]
            ?? configuration["Values:PostgresDb"]
            ?? throw new InvalidOperationException("Database connection string 'PostgresDb' is not configured.");

        // Register DbContext with PostgreSQL
        services.AddDbContext<SpendXDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(120);
            });
        });

        // Register Dependencies
        services.AddScoped<ISpendXRepository, SpendXRepository>();
        services.AddScoped<ISpendXService, SpendXService>();

        // Register FluentValidation Validators
        services.AddScoped<IValidator<EventOfferRequestDto>, EventOfferRequestDtoValidator>();
        services.AddScoped<IValidator<SpendXOverviewRequestDto>, SpendXOverviewRequestDtoValidator>();
        services.AddScoped<IValidator<DataViewRequestDto>, DataViewRequestDtoValidator>();
        services.AddScoped<IValidator<DataViewResponseDto>, DataViewResponseDtoValidator>();
        services.AddScoped<IValidator<SearchByBrandRequestDto>, SearchByBrandRequestDtoValidator>();
        services.AddScoped<IValidator<MoveOfferRequestDto>, MoveOfferRequestDtoValidator>();
        services.AddScoped<IValidator<PricingExtractRequestDto>, PricingExtractRequestDtoValidator>();

        // Logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

host.Run();
