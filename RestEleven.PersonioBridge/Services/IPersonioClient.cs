using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.PersonioBridge.Services;

public interface IPersonioClient
{
    Task<SimpleResult<AttendanceDto>> CreateAttendanceAsync(CreateAttendanceDto request, CancellationToken cancellationToken = default);
}
