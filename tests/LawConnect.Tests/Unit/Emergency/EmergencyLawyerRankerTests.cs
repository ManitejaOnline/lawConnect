using LawConnect.Application.Features.Emergency.Services;

namespace LawConnect.Tests.Unit.Emergency;

public sealed class EmergencyLawyerRankerTests
{
    [Fact]
    public void HigherRating_ScoresHigherThanLowerRating()
    {
        var stronger = EmergencyLawyerRanker.Score(4.9, 0.5, 30, 10);
        var weaker = EmergencyLawyerRanker.Score(3.0, 0.5, 30, 10);
        Assert.True(stronger > weaker);
    }

    [Fact]
    public void HigherRatingAndAcceptance_ImproveScore()
    {
        var stronger = EmergencyLawyerRanker.Score(4.8, 0.9, 20, 15);
        var weaker = EmergencyLawyerRanker.Score(3.0, 0.2, 90, 15);
        Assert.True(stronger > weaker);
    }

    [Fact]
    public void FasterResponseAndMoreExperience_ImproveScore()
    {
        var faster = EmergencyLawyerRanker.Score(4, 0.5, 10, 25);
        var slower = EmergencyLawyerRanker.Score(4, 0.5, 100, 5);
        Assert.True(faster > slower);
    }
}
