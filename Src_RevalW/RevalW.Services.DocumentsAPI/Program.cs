using RevalW.Services.DocumentsAPI.Extensions;
using RevalW.Services.DocumentsAPI.Messaging;
using RevalW.Services.DocumentsAPI.Services;
using RevalW.Services.DocumentsAPI.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;

namespace RevalW.Services.DocumentsAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton(typeof(CommonData), new CommonData { Builder = builder });
        builder.Services.AddSingleton<DocumentsService>();
        builder.Services.AddSingleton<ItemsService>();

        builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
        builder.Services.AddSingleton<AzurePrivateBlobService>();
        builder.Services.AddSingleton<AzurePublicBlobService>();
        builder.Services.AddSingleton<ISimpleHasher, SimpleHasher>();
        builder.Services.AddSingleton<GenerationProgressService>();

        builder.Services.AddHttpClient("TemplatesClient");

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
#if DEBUG
        builder.Services.AddEndpointsApiExplorer();
        builder.AddAppSwaggerGen();
#endif
        builder.AddAppAuthentication();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        //else
        //{
        //    app.UseSwagger();
        //    app.UseSwaggerUI(c =>
        //    {
        //        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DOCUMENTS API");
        //        c.RoutePrefix = string.Empty;
        //    });
        //}

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.UseAzureServiceBusConsumer();
        app.Run();
    }

}
