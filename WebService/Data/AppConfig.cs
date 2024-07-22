namespace BHG.WebService
{
    public class AppConfig
    {
        private static AppConfig _instance = null;

        private AppConfig()
        {
            MongoDBConnectionString = Environment.GetEnvironmentVariable("MongoDB_ConnectionString");
        }

        public static AppConfig GetInstance => _instance ??= new AppConfig();

        public string MongoDBConnectionString { get; protected set; }
    }
}
