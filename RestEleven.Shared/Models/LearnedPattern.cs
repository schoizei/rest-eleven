namespace RestEleven.Shared.Models;

public class LearnedPattern
{
    public DayOfWeek Weekday { get; set; }
    public TimeOnly AvgStart { get; set; }
    public TimeOnly AvgEnd { get; set; }
    public double Confidence { get; set; }
    public int Samples { get; set; }
}
