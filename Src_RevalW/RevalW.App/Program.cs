using Microsoft.AspNetCore.Authentication.Cookies;
using RevalW.App.Mapper;
using RevalW.App.Service;
using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;

namespace RevalW.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddMemoryCache();
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("google", client =>
            {
                client.BaseAddress = new Uri("https://maps.googleapis.com/");
            });

            builder.Services.AddRazorPages();
            builder.Services.AddSingleton(typeof(CommonData), new CommonData { Builder = builder });
            builder.Services.AddScoped<IBaseService, BaseService>();
            builder.Services.AddScoped<ConfigsService>();
            builder.Services.AddScoped<DevicesService>();
            builder.Services.AddScoped<UsersService>();
            builder.Services.AddScoped<ItemsService>();
            builder.Services.AddScoped<ITokenProvider, TokenProvider>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<OrganizationService>();
            builder.Services.AddScoped<OTagsService>();
            builder.Services.AddScoped<ConfigsTagsService>();
            builder.Services.AddScoped<GoogleGeocodingService>();
            builder.Services.AddScoped<CDocumentsService>();
            builder.Services.AddScoped<ISimpleHasher, SimpleHasher>();
            builder.Services.AddSingleton<AzurePrivateBlobService>();
            builder.Services.AddSingleton<AzurePublicBlobService>();

            // SignalR registration for in-app hub
            builder.Services.AddSignalR();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromHours(10);
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                });

            var app = builder.Build();

            ServiceProviderAccessor.Configure(app.Services);
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseStatusCodePagesWithReExecute("/Home/NotFound");

            // ensure authentication middleware is enabled
            app.UseAuthentication();
            app.UseAuthorization();

            // map the in-app SignalR hub used by ViewDetails
            app.MapHub<RevalW.App.Hubs.GenerationHub>("/hubs/generation");

            app.MapRazorPages();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Home}");

            app.Run();
        }
    }
}
