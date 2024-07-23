
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

            string mongoDBConnectionString = builder.Configuration.GetConnectionString("MongoDB");
            if (!string.IsNullOrEmpty(mongoDBConnectionString))
                Environment.SetEnvironmentVariable("MongoDB_ConnectionString", mongoDBConnectionString);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<GameHub>("/game/{roomName}");

            app.Run();
        }
    }
}
