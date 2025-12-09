using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.Client.Services;

public interface IPersonioBridgeClient
{
    Task<SimpleResult<AttendanceDto>> CreateAttendanceAsync(CreateAttendanceDto dto, CancellationToken cancellationToken = default);
}
