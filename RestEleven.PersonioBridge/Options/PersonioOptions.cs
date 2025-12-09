namespace RestEleven.PersonioBridge.Options;

public class PersonioOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string AuthUrl { get; set; } = "https://api.personio.de/v2/auth/token";
    public string AttendanceUrl { get; set; } = "https://api.personio.de/v1/company/attendances";
}
