using RestEleven.Shared.Models;

namespace RestEleven.Shared.Validation;

public static class AttendanceValidation
{
    public static IEnumerable<string> Validate(AttendanceEntry entry)
    {
        if (entry.End <= entry.Start)
        {
            yield return "Ende muss nach dem Start liegen.";
        }

        var worked = (entry.End.ToTimeSpan() - entry.Start.ToTimeSpan()).TotalMinutes;
        if (worked >= 360 && entry.BreakMinutes < 30)
        {
            yield return "Bei Arbeitszeiten über 6 Stunden sind mindestens 30 Minuten Pause empfohlen.";
        }

        if (worked >= 540 && entry.BreakMinutes < 45)
        {
            yield return "Bei Arbeitszeiten über 9 Stunden sind mindestens 45 Minuten Pause empfohlen.";
        }
    }
}
