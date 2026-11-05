using Microsoft.EntityFrameworkCore;
using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Data;
using RevalW.Services.TemplatesAPI.Extensions;
using RevalW.Services.TemplatesAPI.Mapper;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Services.TemplatesAPI.Services;
using RevalW.Services.TemplatesAPI.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.TemplatesAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContext<AppDbContext>(option =>
        {
            //option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        builder.Services.AddAutoMapper(typeof(MappingProfile));
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton(typeof(CommonData), new CommonData { Builder = builder });
        builder.Services.AddScoped<DataTablesWorker<Config, long, ConfigHeaderDto>>();
        builder.Services.AddScoped<ConfigsService>();
        builder.Services.AddScoped<ItemsService>();
        builder.Services.AddScoped<TagsService>();
        builder.Services.AddSingleton<AzurePublicBlobService>();
        builder.Services.AddSingleton<AzurePrivateBlobService>();
        builder.Services.AddSingleton<FileStorageService>();
        builder.Services.AddScoped<BackendApiAuthenticationHttpClientHandler>();
        builder.Services.AddScoped<DocumentsService>();
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        builder.Services.AddHttpClient("ItemsClient").AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();

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
        //        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CONFIGS API");
        //        c.RoutePrefix = string.Empty;
        //    });
        //}

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();
        ApplyMigrations(app);
        app.Run();
    }

    private static void ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Database.GetPendingMigrations().Any()) return;
        db.Database.Migrate();
    }
}
