using Microsoft.OpenApi.Models;
using RestEleven.PushServer.Options;
using RestEleven.PushServer.Services;

namespace RestEleven.PushServer;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<VapidOptions>(builder.Configuration.GetSection("Vapid"));
        builder.Services.AddSingleton<IPushSubscriptionStore, PushSubscriptionStore>();
        builder.Services.AddSingleton<IWebPushService, WebPushService>();
        builder.Services.AddSingleton<WebPush.WebPushClient>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RestEleven Push Server",
                Version = "v1",
                Description = "Verwaltet Browser-Subscriptions und sendet Web-Push-Nachrichten."
            });
        });

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(policy =>
        {
            policy.AddPolicy("Client", cors =>
            {
                if (allowedOrigins.Length == 0)
                {
                    cors.AllowAnyOrigin();
                }
                else
                {
                    cors.WithOrigins(allowedOrigins);
                }

                cors.AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseCors("Client");
        app.UseAuthorization();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
