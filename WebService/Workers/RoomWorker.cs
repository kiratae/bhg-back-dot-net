
using NCrontab;

namespace BHG.WebService
{
    public class RoomWorker : BackgroundService
    {
        private readonly ILogger<RoomWorker> _logger;
        private static readonly CrontabSchedule _schedule = CrontabSchedule.Parse("*/30 * * * *");
        private static DateTime _nextRun;

        public RoomWorker(ILogger<RoomWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string func = "ExecuteAsync";
            try
            {
                await Task.Yield();

                _logger.LogInformation($"{func}: Worker start initializing.");

                _nextRun = DateTime.Now;

                _logger.LogInformation($"{func}: Worker has been initialized.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (DateTime.Now > _nextRun)
                    {
                        DyingMessageGameManager.GetInstance().ClearInactiveSessions();

                        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }

                _logger.LogInformation($"{func}: Work has been stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                throw;
            }
        }
    }
}
