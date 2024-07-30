namespace BHG.WebService
{
    public class AppConfig
    {
        private static AppConfig _instance = null;

        private AppConfig()
        {
            ApiKey = Environment.GetEnvironmentVariable("ApiKey");
        }

        public static AppConfig GetInstance() => _instance ??= new AppConfig();

        public string ApiKey { get; protected set; }
    }
}
