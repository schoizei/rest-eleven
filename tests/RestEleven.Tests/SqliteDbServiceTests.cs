using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestEleven.Client.Data;
using RestEleven.Client.Services;
using RestEleven.Shared.Models;
using Xunit;

namespace RestEleven.Tests;

public class SqliteDbServiceTests : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private IDbContextFactory<AppDbContext>? _factory;

    public async Task InitializeAsync()
    {
        SQLitePCL.Batteries_V2.Init();
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _factory = new PooledFactory(options);
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntry()
    {
        var service = new SqliteDbService(_factory!);
        await service.InitializeAsync();

        var entry = new AttendanceEntry
        {
            Date = new DateOnly(2025, 1, 8),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(16, 30),
            BreakMinutes = 30
        };

        await service.AddAsync(entry);
        var items = await service.GetEntriesAsync();

        Assert.Single(items);
        Assert.Equal(entry.Date, items[0].Date);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingEntity()
    {
        var service = new SqliteDbService(_factory!);
        await service.InitializeAsync();

        var entry = new AttendanceEntry
        {
            Date = new DateOnly(2025, 1, 8),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(16, 0),
            BreakMinutes = 30
        };

        var persisted = await service.AddAsync(entry);
        persisted.Comment = "Updated";
        persisted.End = new TimeOnly(17, 0);

        var updated = await service.UpdateAsync(persisted);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Comment);
        Assert.Equal(new TimeOnly(17, 0), updated.End);
    }

    private sealed class PooledFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public PooledFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AppDbContext(_options));
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }
    }
}
