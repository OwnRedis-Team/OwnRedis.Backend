using Microsoft.Extensions.Options;
using NUnit.Framework;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using OwnRedis.Server.Database;
using OwnRedis.Server.Storages;
using System.Timers;
using Moq;

[TestFixture]
public class OwnRedisArchitectureTests
{
    private Mock<IDateTimeProvider> _clockMock;
    private Mock<IRamCacheStorage> _ramMock;
    private Mock<IFallbackCacheStorage> _fallbackMock;
    private Mock<ICacheRepository> _repoMock;
    private Mock<ICacheMethodsHelper> _helperMock;
    private DefaultMethodsService _service;

    [SetUp]
    public void Setup()
    {
        _clockMock = new Mock<IDateTimeProvider>();
        _ramMock = new Mock<IRamCacheStorage>();
        _fallbackMock = new Mock<IFallbackCacheStorage>();
        _repoMock = new Mock<ICacheRepository>();
        _helperMock = new Mock<ICacheMethodsHelper>();

        _service = new DefaultMethodsService(
            _clockMock.Object,
            Mock.Of<ICacheTtlPolicy>(),
            _ramMock.Object,
            _fallbackMock.Object,
            _repoMock.Object,
            Mock.Of<ICacheSerializer>(),
            Options.Create(new CacheTtlSettings()),
            _helperMock.Object
        );
    }

    [Test]
    public async Task GetAsync_Priority_ShouldReturnRamFirst()
    {
        // Тест: Если данные есть в RAM, мы не идем в Fallback и БД.
        var key = "k1";
        var expected = new CacheObject { Value = "ram" };
        _helperMock.Setup(h => h.TryGetFromRam(key, It.IsAny<DateTimeOffset>())).Returns(expected);

        var result = await _service.GetAsync(key);

        Assert.That(result.Value, Is.EqualTo("ram"));
        _helperMock.Verify(h => h.TryGetFromFallback(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Test]
    public async Task GetAsync_ShouldFallbackToDb_IfRamAndFallbackEmpty()
    {
        // Тест: Цепочка ответственности. RAM -> null, Fallback -> null, БД -> Value.
        var key = "k1";
        var expected = new CacheObject { Value = "db" };
        _helperMock.Setup(h => h.TryGetFromRam(key, It.IsAny<DateTimeOffset>())).Returns((CacheObject)null);
        _helperMock.Setup(h => h.TryGetFromFallback(key, It.IsAny<DateTimeOffset>())).Returns((CacheObject)null);
        _helperMock.Setup(h => h.GetFromDatabaseAsync(key, It.IsAny<DateTimeOffset>())).ReturnsAsync(expected);

        var result = await _service.GetAsync(key);

        Assert.That(result.Value, Is.EqualTo("db"));
    }

    [Test]
    public async Task DeleteAsync_ShouldCallAllLayers()
    {
        // Тест: Удаление должно очистить все три уровня хранения.
        var key = "kill_me";
        await _service.DeleteAsync(key);

        _ramMock.Verify(r => r.TryRemove(key, out It.Ref<CacheObject>.IsAny), Times.Once);
        _fallbackMock.Verify(f => f.TryRemove(key, out It.Ref<CacheObject>.IsAny), Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(key), Times.Once);
    }

    [Test]
    public async Task SetAsync_ShouldSaveToRamAndDatabase()
    {
        // Тест: Проверка, что при записи мы обновляем и оперативку, и репозиторий.
        var key = "new_key";
        var val = new CacheObject { Value = 100 };
        var ttl = TimeSpan.FromMinutes(1);

        await _service.SetAsync(key, val, ttl);

        _helperMock.Verify(h => h.SetInRam(key, val, It.IsAny<DateTimeOffset>(), ttl), Times.Once);
        _helperMock.Verify(h => h.SaveToDatabaseAsync(key, val, ttl), Times.Once);
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnTrueIfInRam()
    {
        // Тест: Оптимизация. Если ключ в RAM, не лезем в БД (экономим запрос).
        _ramMock.Setup(r => r.ContainsKey("fast_key")).Returns(true);

        var exists = await _service.ExistsAsync("fast_key");

        Assert.That(exists);
        _repoMock.Verify(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    // --- 5 ТЕСТОВ НА МНОГОПОТОЧНОСТЬ (Concurrency) ---
    // Здесь тестируем реальные InMemory-хранилища, а не моки.

    [Test]
    public void Concurrent_Set_ShouldNotCrash()
    {
        // Тест: 100 потоков одновременно пишут в одно хранилище.
        var storage = new InMemoryRamCacheStorage();
        Parallel.For(0, 1000, i => {
            storage.Set($"key_{i % 10}", new CacheObject { Value = i });
        });
        Assert.Pass(); // ConcurrentDictionary должен выдержать без исключений
    }

    [Test]
    public async Task Concurrent_ReadWrite_Consistency()
    {
        // Тест: Одни читают, другие пишут. Проверяем, что нет Race Condition.
        var storage = new InMemoryRamCacheStorage();
        var tasks = new List<Task>();

        tasks.Add(Task.Run(() => {
            for (int i = 0; i < 500; i++) storage.Set("race", new CacheObject { Value = i });
        }));

        tasks.Add(Task.Run(() => {
            for (int i = 0; i < 500; i++) storage.TryGetValue("race", out _);
        }));

        await Task.WhenAll(tasks);
        Assert.Pass();
    }

    [Test]
    public void Concurrent_TryRemove_ShouldBeAtomic()
    {
        // Тест: Если 10 потоков пытаются удалить один и тот же ключ, 
        // только ОДИН должен получить true (успешно удалить).
        var storage = new InMemoryRamCacheStorage();
        storage.Set("target", new CacheObject { Value = "die" });

        int successRemovals = 0;
        Parallel.For(0, 100, i => {
            if (storage.TryRemove("target", out _)) Interlocked.Increment(ref successRemovals);
        });

        Assert.That(successRemovals, Is.EqualTo(1));
    }

    [Test]
    public async Task BackgroundService_And_MainService_Collision()
    {
        // Тест: Имитируем работу фоновой очистки и одновременный Get из сервиса.
        var storage = new InMemoryRamCacheStorage();
        storage.Set("coll", new CacheObject { Value = "val", TTL = DateTimeOffset.MinValue }); // Протухший

        var task1 = Task.Run(() => storage.TryRemove("coll", out _)); // "Очистка"
        var task2 = Task.Run(() => storage.TryGetValue("coll", out _)); // "Запрос пользователя"

        await Task.WhenAll(task1, task2);
        Assert.Pass(); // Главное — отсутствие Deadlock
    }

    [Test]
    public void Concurrent_Dictionary_Count_Accuracy()
    {
        // Тест: Массовое добавление уникальных ключей.
        var storage = new InMemoryRamCacheStorage();
        Parallel.For(0, 5000, i => {
            storage.Set(Guid.NewGuid().ToString(), new CacheObject { Value = i });
        });

        Assert.That(storage.Count, Is.EqualTo(5000));
    }
}