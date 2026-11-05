using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RevalW.Services.DocumentsAPI.Messaging;
using System.Text;

namespace RevalW.Services.DocumentsAPI.Extensions;

public static class WebAppBuilderExt
{
    private static IAzureServiceBusConsumer? ServiceBusConsumer { get; set; }

    public static WebApplicationBuilder AddAppAuthentication(this WebApplicationBuilder builder)
    {
        var secret = builder.Configuration.GetValue<string>("JwtOptions:Secret");
        var issuer = builder.Configuration.GetValue<string>("JwtOptions:Issuer");
        var audience = builder.Configuration.GetValue<string>("JwtOptions:Audience");
        var key = Encoding.ASCII.GetBytes(secret!);
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience
            };
        });
        return builder;
    }
    public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
    {
        ServiceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();
        var hostAppLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();

        hostAppLife?.ApplicationStarted.Register(OnStart);
        hostAppLife?.ApplicationStopping.Register(OnStop);
        return app;
    }

    private static void OnStart()
    {
        ServiceBusConsumer?.Start();
    }
    private static void OnStop()
    {
        ServiceBusConsumer?.Stop();
    }


    public static WebApplicationBuilder AddAppSwaggerGen(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen();
        return builder;
    }
}
