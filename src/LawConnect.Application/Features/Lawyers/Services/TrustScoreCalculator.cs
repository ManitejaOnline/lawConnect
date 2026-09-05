namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Pure, side-effect-free scoring functions for the Lawyer Trust Score. Takes plain aggregated
/// numbers so it can be unit tested without a database — <see cref="TrustScoreService"/> is
/// responsible for aggregating raw platform data into the inputs these functions expect.
/// </summary>
/// <remarks>
/// Appointment reliability and chat responsiveness remain appointment/message-derived. Case Updates
/// and Case Management also use those engagement proxies, while stalling deductions come from real
/// Case, Hearing, and Milestone activity via <c>TrustScoreService</c>.
/// Lawyers with no ratings, completed appointments, or cases must not receive an earned Trust level.
/// </remarks>
public static class TrustScoreCalculator
{
    /// <summary>Maximum points for the Client Rating component.</summary>
    public const double MaxClientRating = 25d;

    /// <summary>Maximum points for the Appointment Reliability component.</summary>
    public const double MaxAppointmentReliability = 20d;

    /// <summary>Maximum points for the Responsiveness component.</summary>
    public const double MaxResponsiveness = 15d;

    /// <summary>Maximum points for the Case Updates component.</summary>
    public const double MaxCaseUpdates = 15d;

    /// <summary>Maximum points for the Case Management component.</summary>
    public const double MaxCaseManagement = 15d;

    /// <summary>Maximum points for the Verified Complaints component.</summary>
    public const double MaxVerifiedComplaints = 10d;

    /// <summary>Display level when the lawyer has no ratings, completed appointments, or cases.</summary>
    public const string NewLawyerLevel = "New Lawyer";

    // Bayesian shrinkage: a lawyer with very few reviews is pulled toward this neutral prior
    // rather than immediately outranking a lawyer with hundreds of reviews at a similar average.
    private const double RatingPriorMean = 3.0;
    private const double RatingPriorWeight = 10.0;

    // "Not enough data yet" defaults — documented per component rather than silently using 0 or
    // full marks. Reliability/responsiveness/case components default to 75% (benefit of the doubt,
    // not a fabricated perfect score); Client Rating has no explicit default because the Bayesian
    // formula already resolves a zero-review lawyer to the neutral 3.0/5 prior by construction.
    private const double NoDataFraction = 0.75;

    /// <summary>Client Rating component (0-25). Bayesian-shrunk so a handful of reviews can't outrank a large, consistent history.</summary>
    public static double ClientRatingPoints(double ratingAverage, int ratingCount)
    {
        var count = Math.Max(ratingCount, 0);
        var bayesianAverage = (RatingPriorWeight * RatingPriorMean + ratingAverage * count) / (RatingPriorWeight + count);
        return Math.Clamp(bayesianAverage / 5.0 * MaxClientRating, 0, MaxClientRating);
    }

    /// <summary>
    /// Appointment Reliability component (0-20). Only appointments attributable to the lawyer's own
    /// behavior count — client cancellations, system cancellations, and reschedules are excluded
    /// entirely by the caller (never passed in), not merely un-penalized.
    /// </summary>
    public static double AppointmentReliabilityPoints(int completedCount, int lawyerLateCancellationCount, int noShowCount)
    {
        var total = completedCount + lawyerLateCancellationCount + noShowCount;
        if (total <= 0)
        {
            return MaxAppointmentReliability * NoDataFraction;
        }

        var rate = (double)completedCount / total;
        return Math.Clamp(rate * MaxAppointmentReliability, 0, MaxAppointmentReliability);
    }

    /// <summary>
    /// Maps a lawyer's response gap (in business hours — weekends already excluded by the caller)
    /// to a weight in [0,1] per the spec's Excellent/Good/Average/Poor/VeryPoor bands.
    /// </summary>
    public static double ResponseTimeWeight(double elapsedBusinessHours)
    {
        return elapsedBusinessHours switch
        {
            < 2 => 1.0,   // Excellent
            < 12 => 0.8,  // Good
            < 24 => 0.6,  // Average
            < 48 => 0.3,  // Poor
            _ => 0.1      // Very poor
        };
    }

    /// <summary>Responsiveness component (0-15) from a set of per-message response weights (see <see cref="ResponseTimeWeight"/>).</summary>
    public static double ResponsivenessPoints(IReadOnlyCollection<double> responseWeights)
    {
        if (responseWeights.Count == 0)
        {
            return MaxResponsiveness * NoDataFraction;
        }

        var average = responseWeights.Average();
        return Math.Clamp(average * MaxResponsiveness, 0, MaxResponsiveness);
    }

    /// <summary>
    /// Case Updates component (0-15, proxy). <paramref name="signalsPresent"/> is the sum, across all
    /// completed engagements, of how many of the 3 tracked signals (notes updated, document shared,
    /// post-appointment follow-up) were present; <paramref name="engagementCount"/> is the number of
    /// completed engagements (each worth up to 3 signals).
    /// </summary>
    public static double CaseUpdatePoints(int engagementCount, int signalsPresent)
    {
        var possible = engagementCount * 3;
        if (possible <= 0)
        {
            return MaxCaseUpdates * NoDataFraction;
        }

        var rate = (double)signalsPresent / possible;
        return Math.Clamp(rate * MaxCaseUpdates, 0, MaxCaseUpdates);
    }

    /// <summary>
    /// Case Management component (0-15, proxy). Same shape as <see cref="CaseUpdatePoints"/>: average
    /// of 3 signals (appointment resolved promptly, required documents on file, sustained
    /// communication) across engagements.
    /// </summary>
    public static double CaseManagementPoints(int engagementCount, int signalsPresent)
    {
        var possible = engagementCount * 3;
        if (possible <= 0)
        {
            return MaxCaseManagement * NoDataFraction;
        }

        var rate = (double)signalsPresent / possible;
        return Math.Clamp(rate * MaxCaseManagement, 0, MaxCaseManagement);
    }

    /// <summary>
    /// Verified Complaints component (0-10). Only Verified complaints reach this function — Pending
    /// and Dismissed complaints must never be passed in, by construction of the caller.
    /// </summary>
    public static double VerifiedComplaintPoints(int minorVerifiedCount, int seriousVerifiedCount)
    {
        var deduction = (minorVerifiedCount * 2) + (seriousVerifiedCount * 5);
        return Math.Clamp(MaxVerifiedComplaints - deduction, 0, MaxVerifiedComplaints);
    }

    /// <summary>Blends historical (70%) and recent (30%) values for one component so a single bad stretch can't dominate the score.</summary>
    public static double BlendHistoricalAndRecent(double historical, double recent) => (historical * 0.7) + (recent * 0.3);

    /// <summary>Rounds and clamps the six blended component scores into the final 0-100 Trust Score.</summary>
    public static int TotalScore(
        double clientRating,
        double appointmentReliability,
        double responsiveness,
        double caseUpdates,
        double caseManagement,
        double verifiedComplaints)
    {
        var total = clientRating + appointmentReliability + responsiveness + caseUpdates + caseManagement + verifiedComplaints;
        return (int)Math.Round(Math.Clamp(total, 0, 100), MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// True when there is no earned history to score: no reviews, no completed appointments, and no cases.
    /// </summary>
    public static bool HasInsufficientHistory(int ratingCount, int completedAppointmentCount, int caseCount) =>
        ratingCount <= 0 && completedAppointmentCount <= 0 && caseCount <= 0;

    /// <summary>Maps a 0-100 Trust Score to its display level per the spec's bands.</summary>
    public static string TrustLevel(int score, bool insufficientHistory = false)
    {
        if (insufficientHistory)
        {
            return NewLawyerLevel;
        }

        return score switch
        {
            >= 90 => "Elite Trusted",
            >= 80 => "Highly Trusted",
            >= 70 => "Trusted",
            >= 60 => "Established",
            >= 40 => "Needs Improvement",
            _ => "Low Trust"
        };
    }
}
