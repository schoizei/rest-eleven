using Microsoft.EntityFrameworkCore;
using RestEleven.Client.Data;
using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public class SqliteDbService : ILocalDbService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private bool _initialized;

    public SqliteDbService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        _initialized = true;
    }

    public async Task<IReadOnlyList<AttendanceEntry>> GetEntriesAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.AttendanceEntries.AsNoTracking();
        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.Date <= toDate.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Start)
            .ToList();
    }

    public async Task<AttendanceEntry?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.AttendanceEntries
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.End)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AttendanceEntry> AddAsync(AttendanceEntry entry, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        entry.CreatedUtc = DateTime.UtcNow;

        context.AttendanceEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
        await FlushAsync(context, cancellationToken);
        return entry;
    }

    public async Task<AttendanceEntry?> UpdateAsync(AttendanceEntry entry, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.AttendanceEntries.FirstOrDefaultAsync(e => e.Id == entry.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Date = entry.Date;
        existing.Start = entry.Start;
        existing.End = entry.End;
        existing.BreakMinutes = entry.BreakMinutes;
        existing.Comment = entry.Comment;
        existing.ModifiedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await FlushAsync(context, cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.AttendanceEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.AttendanceEntries.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        await FlushAsync(context, cancellationToken);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await InitializeAsync(cancellationToken);
    }

    private static async Task FlushAsync(DbContext context, CancellationToken cancellationToken)
    {
        var dataSource = context.Database.GetDbConnection().DataSource;
        if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase) || dataSource.StartsWith("file::memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Ensures OPFS-backed database is flushed to disk for durability between sessions.
        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("PRAGMA optimize;", cancellationToken);
    }
}
