using RestEleven.Client.Services;
using RestEleven.Shared.Models;
using Xunit;

namespace RestEleven.Tests;

public class LearningServiceTests
{
    [Fact]
    public void UpdatePattern_ShouldIncreaseSamples()
    {
        var service = new LearningService();
        var monday = new AttendanceEntry
        {
            Date = new DateOnly(2025, 1, 6),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(16, 0)
        };

        var pattern = service.UpdatePattern(monday, 0.3);
        Assert.Equal(DayOfWeek.Monday, pattern.Weekday);
        Assert.Equal(1, pattern.Samples);

        var second = new AttendanceEntry
        {
            Date = monday.Date,
            Start = new TimeOnly(7, 45),
            End = new TimeOnly(16, 15)
        };
        pattern = service.UpdatePattern(second, 0.3);

        Assert.Equal(2, pattern.Samples);
        Assert.True(pattern.Confidence > 0.35);
    }

    [Fact]
    public void BuildSuggestion_ShouldRespectRestRequirement()
    {
        var service = new LearningService();
        var history = new List<AttendanceEntry>
        {
            new()
            {
                Date = new DateOnly(2025, 1, 6),
                Start = new TimeOnly(10, 0),
                End = new TimeOnly(20, 0)
            }
        };

        service.UpdatePattern(new AttendanceEntry
        {
            Date = new DateOnly(2025, 1, 7),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(16, 0)
        });

        var suggestion = service.BuildSuggestion(new DateOnly(2025, 1, 7), history)!;
        var previousEnd = history[0].Date.ToDateTime(history[0].End);
        var suggestedStart = new DateOnly(2025, 1, 7).ToDateTime(suggestion.SuggestedStart);
        var rest = suggestedStart - previousEnd;

        Assert.True(rest.TotalHours >= 11, "Rest window must be >= 11h");
    }
}
