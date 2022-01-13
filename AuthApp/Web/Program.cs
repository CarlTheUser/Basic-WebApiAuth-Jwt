using Application;
using Application.Authentication;
using Data.Common.Contracts;
using Infrastructure.Data.Access;
using Infrastructure.Data.Application;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services to the container.

services.AddControllers();

services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
                                                .SetIsOriginAllowed(origin => true)
                                                .AllowAnyMethod()
                                                .AllowAnyHeader()
                                                .AllowCredentials()
                                                .Build());

    //options.AddPolicy("CorsPolicy", builder => builder.WithOrigins("https://localhost:*").AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]))
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

//services.AddMediatR(typeof(Program).Assembly, typeof(AuthenticatedUser).Assembly);

services.AddTransient<IAuthentication<EmailPasswordAuthCredentials>>(s =>
{
    return new EmailPasswordAuthentication(
        builder.Configuration,
        new UserAccessByEmailQuery(builder.Configuration.GetConnectionString("AccessManagementDb")));
});

services.AddTransient<IRandomStringGenerator, AbcRandomStringGenerator>();

services.AddTransient<IAsyncRepository<Guid, RefreshToken>>(s => new RefreshTokenRepository(builder.Configuration.GetConnectionString("AccessManagementDb")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
