using Access.Repositories;
using Application;
using Application.Authentication;
using Data.Common.Contracts;
using FluentValidation.AspNetCore;
using FluentValidation;
using Infrastructure.Data.Queries;
using Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using Serilog;
using System.Text;
using System.Text.Json;
using Web;
using Web.Models;
using Web.Validators;

static void ConfigureServicesDevelopment(WebApplicationBuilder builder, IServiceCollection services)
{
    services.AddControllers();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var configuration = builder.Configuration;

            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Application:Security:Authentication:Jwt:Issuer"],
                ValidAudience = "This Api",
                IssuerSigningKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]))
            };

            options.Events = new JwtBearerEvents()
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(
                        new ProblemDetails()
                        {
                            Type = $"{context.Request.Scheme}://{context.Request.Host}/errors/unauthorized",
                            Title = "Unauthorized",
                            Detail = "You do not have permission to access this resource.",
                            Instance = context.Request.Path,
                            Status = context.Response.StatusCode
                        }));
                }
            };

            //options.Events = new JwtBearerEvents();

            //options.Events.OnMessageReceived = context => {

            //    if (context != null && context.Request.Cookies.ContainsKey("X-Access-Token"))
            //    {
            //        context.Token = context.Request.Cookies["X-Access-Token"];
            //    }

            //    return Task.CompletedTask;
            //};
        });

    services.AddEndpointsApiExplorer();

    services.AddSwaggerGen();

    services.AddLogging(b =>
    {
        var configuration = builder.Configuration;

        b.AddSerilog(
            logger: Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.MSSqlServer(
                connectionString: configuration.GetConnectionString("AccessManagementDb"),
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions()
                {
                    AutoCreateSqlTable = true,
                    TableName = "AccessManagementDbLogs"
                },
                sinkOptionsSection: null,
                appConfiguration: null,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
            .CreateLogger());
    });

    services.AddTransient<ExceptionHandlingMiddleware>();

    services.AddTransient<IUserAccessRepository>(
        s => new UserAccessRepository(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IRefreshTokenRepository>(
        s => new RefreshTokenRepository(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IAsyncQuery<AuthenticatedUser?, Guid>>(
        s => new AuthenticatedUserByIdQuery(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IAuthentication<EmailPasswordAuthCredentials>>(s =>
    {
        return new EmailPasswordAuthentication(
            userAccessRepository: s.GetRequiredService<IUserAccessRepository>(),
            authenticatedUserByIdQuery: s.GetRequiredService<IAsyncQuery<AuthenticatedUser?, Guid>>());
            
    });

    services.AddTransient<IRandomStringGenerator, AbcRandomStringGenerator>();

    services.AddTransient<ITokenService<EmailPasswordAuthCredentials>, TokenService>();

    services.AddScoped<IValidator<PasswordAuthTokenBindingModel>, PasswordAuthTokenBindingModelValidator>();

    services.AddValidatorsFromAssemblyContaining<PasswordAuthTokenBindingModelValidator>();

    services
        .AddFluentValidationAutoValidation((FluentValidationAutoValidationConfiguration configuration) =>
        {
            configuration.DisableDataAnnotationsValidation = true;
            //configuration.ImplicitlyValidateChildProperties = true;
        })
        .AddFluentValidationClientsideAdapters((FluentValidationClientModelValidatorProvider provider) =>
        {

        });
}

static void ConfigureServicesProduction(WebApplicationBuilder builder, IServiceCollection services)
{
    services.AddControllers();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var configuration = builder.Configuration;

            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Application:Security:Authentication:Jwt:Issuer"],
                ValidAudience = "This Api",
                IssuerSigningKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]))
            };

            options.Events = new JwtBearerEvents()
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(
                        new ProblemDetails()
                        {
                            Type = $"{context.Request.Scheme}://{context.Request.Host}/errors/unauthorized",
                            Title = "Unauthorized",
                            Detail = "You do not have permission to access this resource.",
                            Instance = context.Request.Path,
                            Status = context.Response.StatusCode
                        }));
                }
            };

            //options.Events = new JwtBearerEvents();

            //options.Events.OnMessageReceived = context => {

            //    if (context != null && context.Request.Cookies.ContainsKey("X-Access-Token"))
            //    {
            //        context.Token = context.Request.Cookies["X-Access-Token"];
            //    }

            //    return Task.CompletedTask;
            //};
        });

    services.AddEndpointsApiExplorer();

    services.AddSwaggerGen();

    services.AddLogging(b =>
    {
        var configuration = builder.Configuration;

        b.AddSerilog(
            logger: Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.MSSqlServer(
                connectionString: configuration.GetConnectionString("AccessManagementDb"),
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions()
                {
                    AutoCreateSqlTable = true,
                    TableName = "AccessManagementDbLogs"
                },
                sinkOptionsSection: null,
                appConfiguration: null,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
            .CreateLogger());
    });

    services.AddTransient<ExceptionHandlingMiddleware>();

    services.AddTransient<IUserAccessRepository>(
        s => new UserAccessRepository(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IRefreshTokenRepository>(
        s => new RefreshTokenRepository(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IAsyncQuery<AuthenticatedUser?, Guid>>(
        s => new AuthenticatedUserByIdQuery(
            connectionString: builder.Configuration.GetConnectionString("AccessManagementDb"),
            commandTimeout: builder.Configuration.GetValue<int>("Infrastructure:Data:Sql:CommandTimeout")));

    services.AddTransient<IAuthentication<EmailPasswordAuthCredentials>>(s =>
    {
        return new EmailPasswordAuthentication(
            userAccessRepository: s.GetRequiredService<IUserAccessRepository>(),
            authenticatedUserByIdQuery: s.GetRequiredService<IAsyncQuery<AuthenticatedUser?, Guid>>());

    });

    services.AddTransient<IRandomStringGenerator, AbcRandomStringGenerator>();

    services.AddTransient<ITokenService<EmailPasswordAuthCredentials>, TokenService>();

    services.AddScoped<IValidator<PasswordAuthTokenBindingModel>, PasswordAuthTokenBindingModelValidator>();

    services.AddValidatorsFromAssemblyContaining<PasswordAuthTokenBindingModelValidator>();

    services
        .AddFluentValidationAutoValidation((FluentValidationAutoValidationConfiguration configuration) =>
        {
            configuration.DisableDataAnnotationsValidation = true;
            //configuration.ImplicitlyValidateChildProperties = true;
        })
        .AddFluentValidationClientsideAdapters((FluentValidationClientModelValidatorProvider provider) =>
        {

        });
}

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services to the container.

switch (builder.Environment.EnvironmentName)
{
    case "Development":
        ConfigureServicesDevelopment(builder, services);
        break;
    case "Production":
    default:
        ConfigureServicesProduction(builder, services);
        break;
}

builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(builder => builder
    .SetIsOriginAllowed(origin => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
    .Build());

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
