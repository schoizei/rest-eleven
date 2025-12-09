using Microsoft.OpenApi.Models;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;
using RestEleven.PersonioBridge.Options;
using RestEleven.PersonioBridge.Services;

namespace RestEleven.PersonioBridge;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<PersonioOptions>(builder.Configuration.GetSection("Personio"));

        builder.Services.AddHttpClient<IPersonioClient, PersonioClient>()
            .AddPolicyHandler(GetRetryPolicy());

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RestEleven Personio Bridge",
                Version = "v1",
                Description = "Serverseitige Brücke zur Personio Attendance API"
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

                cors.AllowAnyHeader().AllowAnyMethod();
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

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var delays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 3);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(delays);
    }
}
