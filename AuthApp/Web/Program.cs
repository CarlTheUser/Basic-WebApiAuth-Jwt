using Access;
using Application;
using Application.Authentication;
using Data.Common.Contracts;
using Infrastructure.Data.Access;
using Infrastructure.Data.Application;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using Serilog;
using System.Text;
using System.Text.Json;
using Web;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services to the container.

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddLogging(b =>
{
    var configuration = builder.Configuration;

    b.AddSerilog(Log.Logger
        = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .MinimumLevel.Verbose()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console()
        .CreateLogger());
});

services.AddMediatR(typeof(Program).Assembly, typeof(AuthenticatedUser).Assembly);

services.AddTransient<ExceptionHandlingMiddleware>();

services.AddTransient<IAuthentication<EmailPasswordAuthCredentials>>(s =>
{
    return new EmailPasswordAuthentication(
        builder.Configuration,
        new UserAccessByEmailQuery(builder.Configuration.GetConnectionString("AccessManagementDb")));
});

services.AddTransient<IRandomStringGenerator, AbcRandomStringGenerator>();

services.AddTransient<IAsyncRepository<RefreshToken>>(s => new RefreshTokenRepository(builder.Configuration.GetConnectionString("AccessManagementDb")));

//services.AddTransient<IAsyncQuery<UserAccess?, Guid>>(s => new UserAccessRepository(builder.Configuration.GetConnectionString("AccessManagementDb")));

//services.AddTransient<IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter>>(s => new RefreshTokenByUserValueQuery(builder.Configuration.GetConnectionString("AccessManagementDb")));

var app = builder.Build();

// Configure the HTTP request pipeline.
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
