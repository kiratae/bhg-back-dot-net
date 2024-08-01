
using NCrontab;

namespace BHG.WebService
{
    public class RoomWorker : BackgroundService
    {
        private readonly ILogger<RoomWorker> _logger;
        private static readonly CrontabSchedule _schedule = CrontabSchedule.Parse("0 */1 * * *");
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

                _logger.LogInformation("{func}: Room worker start initializing.", func);

                _nextRun = DateTime.Now;
                var instance = DyingMessageGameManager.GetInstance();

                _logger.LogInformation("{func}: Room worker has been initialized.", func);

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (DateTime.Now > _nextRun)
                    {
                        instance.ClearInactiveSessions();

                        _logger.LogInformation("{func}: Current active {acitveRoomCount} rooms.", func, instance.GetActiveSession().Count);

                        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }

                _logger.LogInformation("{func}: Room worker has been stopped.", func);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{func}: Exception caugth.", func);
                throw;
            }
        }
    }
}
