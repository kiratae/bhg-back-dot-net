

namespace BHG.WebService
{
    public class Program
    {
        public const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddHostedService<RoomWorker>();

            builder.Services.AddSignalR();

            builder.Services.AddLogging(builder => builder.AddConsole());

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                                  });
            });

            builder.Services.AddHealthChecks();

            string apiKey = builder.Configuration.GetValue<string>("ApiKey");
            if (!string.IsNullOrEmpty(apiKey))
                Environment.SetEnvironmentVariable("ApiKey", apiKey);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors(MyAllowSpecificOrigins);
            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<ApiKeyMiddleware>();

            app.MapControllers();

            app.MapHub<GameHub>("/game/{roomCode:regex(\\w+-\\w+-\\w+)}");

            app.MapHealthChecks("/healthz");

            app.Run();
        }
    }
}
