using System;
using System.Reflection;
using System.Threading.Tasks;
using Conduit.Features.Profiles;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Conduit;

public static class ServicesExtensions
{
    public static void AddConduit(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
        );
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddScoped(
            typeof(IPipelineBehavior<,>),
            typeof(DBContextTransactionPipelineBehavior<,>)
        );

        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddAutoMapper(typeof(Program));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IProfileReader, ProfileReader>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    }

    public static void AddJwt(this IServiceCollection services)
    {
        services.AddOptions();

        var signingKey = new SymmetricSecurityKey(
            "somethinglongerforthisdumbalgorithmisrequired"u8.ToArray()
        );
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var issuer = "issuer";
        var audience = "audience";

        services.Configure<JwtIssuerOptions>(options =>
        {
            options.Issuer = issuer;
            options.Audience = audience;
            options.SigningCredentials = signingCredentials;
        });

        var tokenValidationParameters = new TokenValidationParameters
        {
            // The signing key must match!
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingCredentials.Key,
            // Validate the JWT Issuer (iss) claim
            ValidateIssuer = true,
            ValidIssuer = issuer,
            // Validate the JWT Audience (aud) claim
            ValidateAudience = true,
            ValidAudience = audience,
            // Validate the token expiry
            ValidateLifetime = true,
            // If you want to allow a certain amount of clock drift, set that here:
            ClockSkew = TimeSpan.Zero
        };

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = (context) =>
                    {
                        var token = context.HttpContext.Request.Headers.Authorization.ToString();
                        if (token.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = token["Token ".Length..].Trim();
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }

    public static void AddSerilogLogging(this ILoggerFactory loggerFactory)
    {
        // Attach the sink to the logger configuration
        var log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            //just for local debug
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {SourceContext} {Message}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code
            )
            .CreateLogger();

        loggerFactory.AddSerilog(log);
        Log.Logger = log;
    }
}
