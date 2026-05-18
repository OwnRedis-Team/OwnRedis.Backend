using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects.Snapshot;
using OwnRedis.Server.Database;
using System.Text.Json;

namespace OwnRedis.Server.Services.TTL
{
    public class CacheSnapshotBackgroundService : BackgroundService
    {
        private readonly IRamCacheStorage _ram;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<SnapshotSettings> _settings;

        public CacheSnapshotBackgroundService(
            IRamCacheStorage ram,
            IDateTimeProvider clock,
            IOptions<SnapshotSettings> settings)
        {
            _ram = ram;
            _clock = clock;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TakeSnapshotAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    AdminLogger.Log($"Ошибка при создании снепшота кеша: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.Value.IntervalSeconds), stoppingToken);
            }
        }

        private async Task TakeSnapshotAsync(CancellationToken stoppingToken)
        {
            var snapshotTime = _clock.UtcNow;
            var items = new List<SnapshotItem>();

            // RAM
            foreach (var kvp in _ram.GetAll())
            {
                items.Add(new SnapshotItem
                {
                    Key = kvp.Key,
                    Value = kvp.Value.Value,
                    TTL = kvp.Value.TTL,
                });
            }

            var snapshot = new
            {
                SnapshotTime = snapshotTime,
                Items = items
            };

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            var filePath = _settings.Value.FilePath;
            await File.WriteAllTextAsync(filePath, json, stoppingToken);

            AdminLogger.Log($"Снепшот кеша сохранён в {filePath}. Всего записей: {snapshot.Items.Count}");
        }
    }
}
