using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public class LearningService : ILearningService
{
    private const double DefaultAlpha = 0.3;
    private static readonly TimeSpan RestRequirement = TimeSpan.FromHours(11);
    private readonly Dictionary<DayOfWeek, LearnedPattern> _patterns = new();

    public LearnedPattern UpdatePattern(AttendanceEntry entry, double alpha = DefaultAlpha)
    {
        var weekday = entry.Date.DayOfWeek;
        alpha = Math.Clamp(alpha, 0.05, 0.9);

        if (!_patterns.TryGetValue(weekday, out var pattern))
        {
            pattern = new LearnedPattern
            {
                Weekday = weekday,
                AvgStart = entry.Start,
                AvgEnd = entry.End,
                Samples = 1,
                Confidence = 0.35
            };
            _patterns[weekday] = pattern;
            return pattern;
        }

        pattern.AvgStart = Blend(pattern.AvgStart, entry.Start, alpha);
        pattern.AvgEnd = Blend(pattern.AvgEnd, entry.End, alpha);
        pattern.Samples += 1;
        pattern.Confidence = CalculateConfidence(pattern.Samples, alpha);
        return pattern;
    }

    public LearnedPattern? GetPattern(DayOfWeek dayOfWeek)
    {
        _patterns.TryGetValue(dayOfWeek, out var pattern);
        return pattern;
    }

    public LearningSuggestion? BuildSuggestion(DateOnly targetDate, IReadOnlyList<AttendanceEntry> history, double alpha = DefaultAlpha)
    {
        var pattern = GetPattern(targetDate.DayOfWeek);
        if (pattern is null)
        {
            if (history.Count == 0)
            {
                return null;
            }

            var last = history.OrderByDescending(e => e.Date).ThenByDescending(e => e.End).First();
            var fallbackStart = AddWithClamp(last.End, RestRequirement);
            return new LearningSuggestion(fallbackStart, AddWithClamp(fallbackStart, TimeSpan.FromHours(8)), 0.25, false);
        }

        var candidateStart = pattern.AvgStart;
        var candidateEnd = pattern.AvgEnd;
        var respectsRest = CheckRestConstraint(targetDate, candidateStart, history);
        var adjustedStart = candidateStart;
        var adjustedEnd = candidateEnd;
        double confidence = pattern.Confidence;

        if (!respectsRest)
        {
            var previous = FindPreviousEntry(targetDate, history);
            if (previous is not null)
            {
                var earliest = AddWithClamp(previous.End, RestRequirement);
                if (earliest > adjustedStart)
                {
                    adjustedStart = earliest;
                    var duration = pattern.AvgEnd.ToTimeSpan() - pattern.AvgStart.ToTimeSpan();
                    if (duration <= TimeSpan.Zero)
                    {
                        duration = TimeSpan.FromHours(8);
                    }

                    adjustedEnd = AddWithClamp(adjustedStart, duration);
                }
            }

            confidence *= 0.75;
        }

        return new LearningSuggestion(adjustedStart, adjustedEnd, Math.Round(confidence, 2), respectsRest);
    }

    private static bool CheckRestConstraint(DateOnly targetDate, TimeOnly suggestedStart, IReadOnlyList<AttendanceEntry> history)
    {
        var previous = FindPreviousEntry(targetDate, history);
        if (previous is null)
        {
            return true;
        }

        var previousEnd = previous.Date.ToDateTime(previous.End);
        var candidateStart = targetDate.ToDateTime(suggestedStart);
        return candidateStart - previousEnd >= RestRequirement;
    }

    private static AttendanceEntry? FindPreviousEntry(DateOnly targetDate, IReadOnlyList<AttendanceEntry> history)
    {
        return history
            .Where(e => e.Date < targetDate)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.End)
            .FirstOrDefault();
    }

    private static TimeOnly Blend(TimeOnly current, TimeOnly sample, double alpha)
    {
        var minutes = current.ToTimeSpan().TotalMinutes * (1 - alpha) + sample.ToTimeSpan().TotalMinutes * alpha;
        return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));
    }

    private static double CalculateConfidence(int samples, double alpha)
    {
        var baseline = 1 - Math.Exp(-(samples * alpha));
        return Math.Clamp(baseline, 0.2, 0.99);
    }

    private static TimeOnly AddWithClamp(TimeOnly value, TimeSpan delta)
    {
        var minutes = value.ToTimeSpan().TotalMinutes + delta.TotalMinutes;
        minutes = Math.Clamp(minutes, 0, (24 * 60) - 1);
        return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));
    }
}
