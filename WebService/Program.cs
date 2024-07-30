
namespace BHG.WebService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSignalR();

            string apiKey = builder.Configuration.GetValue<string>("ApiKey");
            if (!string.IsNullOrEmpty(apiKey))
                Environment.SetEnvironmentVariable("ApiKey", apiKey);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

            }

            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMiddleware<ApiKeyMiddleware>();

            app.MapControllers();

            app.MapHub<GameHub>("/game/{roomCode}");

            app.MapGet("/health_check", () => "App online");

            app.Run();
        }
    }
}
