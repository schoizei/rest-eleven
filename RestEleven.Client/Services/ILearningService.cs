using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public record LearningSuggestion(TimeOnly SuggestedStart, TimeOnly SuggestedEnd, double Confidence, bool RespectsRestConstraint);

public interface ILearningService
{
    LearnedPattern UpdatePattern(AttendanceEntry entry, double alpha = 0.3);
    LearnedPattern? GetPattern(DayOfWeek dayOfWeek);
    LearningSuggestion? BuildSuggestion(DateOnly targetDate, IReadOnlyList<AttendanceEntry> history, double alpha = 0.3);
}
