using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public interface ILocalDbService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceEntry>> GetEntriesAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
    Task<AttendanceEntry?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<AttendanceEntry> AddAsync(AttendanceEntry entry, CancellationToken cancellationToken = default);
    Task<AttendanceEntry?> UpdateAsync(AttendanceEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
