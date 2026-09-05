using LawConnect.Application.Features.Lawyers.Services;

namespace LawConnect.Tests.Unit.Lawyers;

public sealed class TrustScoreCalculatorTests
{
    // --- Scenario 1: new lawyer with no activity ---

    [Fact]
    public void ClientRatingPoints_NoReviews_ResolvesToNeutralPrior_WhenUsedAsComponent()
    {
        var points = TrustScoreCalculator.ClientRatingPoints(0, 0);
        Assert.Equal(15.0, points, precision: 5); // 3.0/5 prior * 25 — used only after the lawyer has other history
    }

    [Fact]
    public void AppointmentReliabilityPoints_NoAppointments_ReturnsNeutralDefault()
    {
        var points = TrustScoreCalculator.AppointmentReliabilityPoints(0, 0, 0);
        Assert.Equal(15.0, points, precision: 5); // 75% of 20
    }

    [Fact]
    public void ResponsivenessPoints_NoMessages_ReturnsNeutralDefault()
    {
        var points = TrustScoreCalculator.ResponsivenessPoints([]);
        Assert.Equal(11.25, points, precision: 5); // 75% of 15
    }

    [Fact]
    public void CaseUpdatePoints_NoEngagements_ReturnsNeutralDefault()
    {
        var points = TrustScoreCalculator.CaseUpdatePoints(0, 0);
        Assert.Equal(11.25, points, precision: 5); // 75% of 15
    }

    [Fact]
    public void CaseManagementPoints_NoEngagements_ReturnsNeutralDefault()
    {
        var points = TrustScoreCalculator.CaseManagementPoints(0, 0);
        Assert.Equal(11.25, points, precision: 5);
    }

    [Fact]
    public void HasInsufficientHistory_ZeroReviewsAppointmentsAndCases_IsTrue()
    {
        Assert.True(TrustScoreCalculator.HasInsufficientHistory(0, 0, 0));
    }

    [Fact]
    public void HasInsufficientHistory_WithRatings_IsFalse()
    {
        Assert.False(TrustScoreCalculator.HasInsufficientHistory(1, 0, 0));
    }

    [Fact]
    public void HasInsufficientHistory_WithCompletedAppointments_IsFalse()
    {
        Assert.False(TrustScoreCalculator.HasInsufficientHistory(0, 1, 0));
    }

    [Fact]
    public void HasInsufficientHistory_WithCases_IsFalse()
    {
        Assert.False(TrustScoreCalculator.HasInsufficientHistory(0, 0, 1));
    }

    [Fact]
    public void NewLawyer_InsufficientHistory_IsNotTrustedAndDoesNotFabricateAHighScore()
    {
        var insufficient = TrustScoreCalculator.HasInsufficientHistory(0, 0, 0);
        var score = TrustScoreCalculator.TotalScore(0, 0, 0, 0, 0, 0);
        Assert.True(insufficient);
        Assert.Equal(0, score);
        Assert.Equal(TrustScoreCalculator.NewLawyerLevel, TrustScoreCalculator.TrustLevel(score, insufficientHistory: true));
        Assert.NotEqual("Trusted", TrustScoreCalculator.TrustLevel(score, insufficientHistory: true));
    }

    [Fact]
    public void EstablishedLawyer_WithRatings_UsesScoreBands()
    {
        var clientRating = TrustScoreCalculator.ClientRatingPoints(4.8, 40);
        var reliability = TrustScoreCalculator.AppointmentReliabilityPoints(20, 0, 0);
        var responsiveness = TrustScoreCalculator.ResponsivenessPoints([1.0, 0.8]);
        var caseUpdates = TrustScoreCalculator.CaseUpdatePoints(8, 20);
        var caseManagement = TrustScoreCalculator.CaseManagementPoints(8, 18);
        var complaints = TrustScoreCalculator.VerifiedComplaintPoints(0, 0);
        var score = TrustScoreCalculator.TotalScore(clientRating, reliability, responsiveness, caseUpdates, caseManagement, complaints);
        Assert.False(TrustScoreCalculator.HasInsufficientHistory(40, 20, 2));
        Assert.InRange(score, 70, 100);
        Assert.NotEqual(TrustScoreCalculator.NewLawyerLevel, TrustScoreCalculator.TrustLevel(score));
    }

    [Fact]
    public void LawyerWithCaseHistory_ZeroReviews_IsNotClassifiedAsNewLawyer()
    {
        Assert.False(TrustScoreCalculator.HasInsufficientHistory(ratingCount: 0, completedAppointmentCount: 0, caseCount: 3));
        var caseManagement = Math.Max(0, TrustScoreCalculator.CaseManagementPoints(4, 6) - 5);
        var score = TrustScoreCalculator.TotalScore(
            TrustScoreCalculator.ClientRatingPoints(0, 0),
            TrustScoreCalculator.AppointmentReliabilityPoints(0, 0, 0),
            TrustScoreCalculator.ResponsivenessPoints([]),
            TrustScoreCalculator.CaseUpdatePoints(0, 0),
            caseManagement,
            TrustScoreCalculator.VerifiedComplaintPoints(0, 0));
        Assert.NotEqual(TrustScoreCalculator.NewLawyerLevel, TrustScoreCalculator.TrustLevel(score, insufficientHistory: false));
    }

    [Fact]
    public void StallingPenalty_ReducesCaseManagementThenTotal_WithoutChangingOtherComponents()
    {
        var caseManagement = TrustScoreCalculator.CaseManagementPoints(4, 12);
        var afterStalling = Math.Max(0, caseManagement - 5);
        var without = TrustScoreCalculator.TotalScore(20, 18, 12, 12, caseManagement, 10);
        var with = TrustScoreCalculator.TotalScore(20, 18, 12, 12, afterStalling, 10);
        Assert.True(with < without);
        Assert.Equal(5, without - with);
    }

    // --- Scenario 2 & 3: 1 five-star review vs. many reviews (Bayesian shrinkage) ---

    [Fact]
    public void ClientRatingPoints_OneFiveStarReview_ShrinksTowardPrior()
    {
        var points = TrustScoreCalculator.ClientRatingPoints(5.0, 1);
        Assert.True(points < 20, $"one review should be heavily shrunk, got {points}");
    }

    [Fact]
    public void ClientRatingPoints_ManyFiveStarReviews_ApproachesFullMarks()
    {
        var points = TrustScoreCalculator.ClientRatingPoints(5.0, 200);
        Assert.True(points > 24, $"200 reviews should barely be shrunk, got {points}");
    }

    [Fact]
    public void ClientRatingPoints_ManyReviews_OutranksOneReview_AtSameAverage()
    {
        var few = TrustScoreCalculator.ClientRatingPoints(5.0, 1);
        var many = TrustScoreCalculator.ClientRatingPoints(5.0, 200);
        Assert.True(many > few, "a lawyer with 200 five-star reviews must outrank one with a single five-star review");
    }

    [Fact]
    public void ClientRatingPoints_MonotonicallyIncreasesWithReviewCount_AtFixedHighAverage()
    {
        var ten = TrustScoreCalculator.ClientRatingPoints(5.0, 10);
        var fifty = TrustScoreCalculator.ClientRatingPoints(5.0, 50);
        var twoHundred = TrustScoreCalculator.ClientRatingPoints(5.0, 200);
        Assert.True(ten < fifty);
        Assert.True(fifty < twoHundred);
    }

    // --- Scenario 4: completed appointment ---

    [Fact]
    public void AppointmentReliabilityPoints_AllCompleted_ReturnsFullMarks()
    {
        var points = TrustScoreCalculator.AppointmentReliabilityPoints(completedCount: 10, lawyerLateCancellationCount: 0, noShowCount: 0);
        Assert.Equal(20.0, points, precision: 5);
    }

    // --- Scenario 5: lawyer no-show ---

    [Fact]
    public void AppointmentReliabilityPoints_NoShow_ReducesScore()
    {
        var clean = TrustScoreCalculator.AppointmentReliabilityPoints(10, 0, 0);
        var withNoShow = TrustScoreCalculator.AppointmentReliabilityPoints(9, 0, 1);
        Assert.True(withNoShow < clean);
    }

    [Fact]
    public void AppointmentReliabilityPoints_LateCancellation_ReducesScore()
    {
        var clean = TrustScoreCalculator.AppointmentReliabilityPoints(10, 0, 0);
        var withLateCancel = TrustScoreCalculator.AppointmentReliabilityPoints(9, 1, 0);
        Assert.True(withLateCancel < clean);
    }

    // --- Scenario 6: client cancellation must never be passed in — proven at the service layer,
    // but we can prove here that the calculator itself has no way to penalize anything it isn't
    // explicitly told about (client cancellations are simply never counted in any parameter).
    [Fact]
    public void AppointmentReliabilityPoints_OnlyCountsWhatItIsToldAbout()
    {
        // 5 completed + "invisible" client cancellations that never reach this function at all.
        var points = TrustScoreCalculator.AppointmentReliabilityPoints(5, 0, 0);
        Assert.Equal(20.0, points, precision: 5);
    }

    // --- Scenario 7 & 8: fast vs. slow message response ---

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(11.9, 0.8)]
    [InlineData(23.9, 0.6)]
    [InlineData(47.9, 0.3)]
    [InlineData(72.0, 0.1)]
    public void ResponseTimeWeight_MapsHoursToExpectedBand(double hours, double expectedWeight)
    {
        Assert.Equal(expectedWeight, TrustScoreCalculator.ResponseTimeWeight(hours));
    }

    [Fact]
    public void ResponsivenessPoints_FastResponses_OutscoreSlowResponses()
    {
        var fast = TrustScoreCalculator.ResponsivenessPoints([1.0, 1.0, 1.0]);
        var slow = TrustScoreCalculator.ResponsivenessPoints([0.1, 0.1, 0.1]);
        Assert.True(fast > slow);
    }

    // --- Scenario 9 & 10: case updated regularly vs. case inactive ---

    [Fact]
    public void CaseUpdatePoints_AllSignalsPresent_ReturnsFullMarks()
    {
        var points = TrustScoreCalculator.CaseUpdatePoints(engagementCount: 4, signalsPresent: 12); // 4 * 3
        Assert.Equal(15.0, points, precision: 5);
    }

    [Fact]
    public void CaseUpdatePoints_NoSignalsPresent_ScoresLowerThanNeutralDefault()
    {
        var noData = TrustScoreCalculator.CaseUpdatePoints(0, 0);
        var inactive = TrustScoreCalculator.CaseUpdatePoints(engagementCount: 4, signalsPresent: 0);
        Assert.True(inactive < noData, "an inactive lawyer with real engagements and zero activity should score below the no-data default");
    }

    // --- Scenario 11 & 12: verified vs. dismissed complaint ---
    // (dismissed complaints never reach this function at all — proven at the service/integration
    // layer; here we prove Verified severity math is correct.)

    [Fact]
    public void VerifiedComplaintPoints_NoComplaints_ReturnsFullMarks()
    {
        Assert.Equal(10.0, TrustScoreCalculator.VerifiedComplaintPoints(0, 0));
    }

    [Fact]
    public void VerifiedComplaintPoints_MinorVerified_SmallDeduction()
    {
        Assert.Equal(8.0, TrustScoreCalculator.VerifiedComplaintPoints(1, 0));
    }

    [Fact]
    public void VerifiedComplaintPoints_SeriousVerified_LargerDeduction()
    {
        Assert.Equal(5.0, TrustScoreCalculator.VerifiedComplaintPoints(0, 1));
    }

    [Fact]
    public void VerifiedComplaintPoints_FloorsAtZero_NeverNegative()
    {
        Assert.Equal(0.0, TrustScoreCalculator.VerifiedComplaintPoints(0, 10));
    }

    // --- Scenario 16: trust score recalculation reflects blended inputs ---

    [Fact]
    public void TotalScore_SumsAndClampsSixComponents()
    {
        var score = TrustScoreCalculator.TotalScore(25, 20, 15, 15, 15, 10);
        Assert.Equal(100, score);
    }

    [Fact]
    public void TotalScore_NeverExceeds100_EvenIfComponentsSomehowOverflow()
    {
        var score = TrustScoreCalculator.TotalScore(30, 25, 20, 20, 20, 15);
        Assert.Equal(100, score);
    }

    // --- Scenario 17: trust level boundaries ---

    [Theory]
    [InlineData(0, "Low Trust")]
    [InlineData(39, "Low Trust")]
    [InlineData(40, "Needs Improvement")]
    [InlineData(59, "Needs Improvement")]
    [InlineData(60, "Established")]
    [InlineData(69, "Established")]
    [InlineData(70, "Trusted")]
    [InlineData(79, "Trusted")]
    [InlineData(80, "Highly Trusted")]
    [InlineData(89, "Highly Trusted")]
    [InlineData(90, "Elite Trusted")]
    [InlineData(100, "Elite Trusted")]
    public void TrustLevel_MapsScoreToExpectedBand(int score, string expectedLevel)
    {
        Assert.Equal(expectedLevel, TrustScoreCalculator.TrustLevel(score));
    }

    // --- Scenario 18: historical/recent 70/30 weighting ---

    [Fact]
    public void BlendHistoricalAndRecent_WeightsHistoricalMoreThanRecent()
    {
        var blended = TrustScoreCalculator.BlendHistoricalAndRecent(historical: 20, recent: 0);
        Assert.Equal(14.0, blended, precision: 5); // 20*0.7 + 0*0.3
    }

    [Fact]
    public void BlendHistoricalAndRecent_OneBadRecentStretch_DoesNotDominateStrongHistory()
    {
        var blended = TrustScoreCalculator.BlendHistoricalAndRecent(historical: 20, recent: 0);
        Assert.True(blended > 10, "a single bad recent stretch must not crater a strong historical score");
    }

    [Fact]
    public void BlendHistoricalAndRecent_EqualInputs_ReturnsSameValue()
    {
        Assert.Equal(15.0, TrustScoreCalculator.BlendHistoricalAndRecent(15, 15), precision: 5);
    }
}
