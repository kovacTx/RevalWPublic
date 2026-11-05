using Microsoft.EntityFrameworkCore;
using RevalW.MessageBus;
using RevalW.Services.ItemsAPI.Data;
using RevalW.Services.ItemsAPI.Extensions;
using RevalW.Services.ItemsAPI.Mapper;
using RevalW.Services.ItemsAPI.Model;
using RevalW.Services.ItemsAPI.Services;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;

namespace RevalW.Services.ItemsAPI;

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
        builder.Services.AddScoped<DataTablesWorker<Item, long, ItemHeaderDto>>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<ItemsService>();
        builder.Services.AddScoped<CommonDataService>();
        builder.Services.AddScoped<TagsService>();
        builder.Services.AddScoped<CTagsService>();
        builder.Services.AddScoped<BackendApiAuthenticationHttpClientHandler>();
        builder.Services.AddScoped<CDocumentsService>();
        builder.Services.AddScoped<DocumentsService>();
        builder.Services.AddScoped<OrganizationService>();
        builder.Services.AddScoped<DevicesService>();
        builder.Services.AddSingleton<AzurePrivateBlobService>();
        builder.Services.AddScoped<IMessageBus, MessageBus.MessageBus>();
        builder.Services.AddScoped<ISimpleHasher, SimpleHasher>();
        builder.Services.AddScoped<Thorizer>();
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        builder.Services.AddHttpClient("TemplatesClient").AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();
        builder.Services.AddHttpClient("DocumentsClient").AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();
        builder.Services.AddHttpClient("AuthClient").AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();
        builder.Services.AddHttpClient("DevicesClient").AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();

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
        //        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ITEMS API");
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
