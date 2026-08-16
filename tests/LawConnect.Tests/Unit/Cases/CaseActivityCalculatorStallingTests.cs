using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;

namespace LawConnect.Tests.Unit.Cases;

public sealed class CaseActivityCalculatorStallingTests
{
    // --- ComputeStallingStatus(status, days) threshold boundaries ---

    [Theory]
    [InlineData(0, CaseStallingStatus.None)]
    [InlineData(6, CaseStallingStatus.None)]
    [InlineData(7, CaseStallingStatus.Warning)]
    [InlineData(13, CaseStallingStatus.Warning)]
    [InlineData(14, CaseStallingStatus.Stalling)]
    [InlineData(29, CaseStallingStatus.Stalling)]
    [InlineData(30, CaseStallingStatus.SevereStalling)]
    [InlineData(90, CaseStallingStatus.SevereStalling)]
    public void ComputeStallingStatus_ActiveCase_ClassifiesByDayThreshold(int days, CaseStallingStatus expected)
    {
        var result = CaseActivityCalculator.ComputeStallingStatus(CaseStatus.Active, days);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(CaseStatus.Won)]
    [InlineData(CaseStatus.Lost)]
    [InlineData(CaseStatus.Settled)]
    [InlineData(CaseStatus.Closed)]
    [InlineData(CaseStatus.Cancelled)]
    public void ComputeStallingStatus_TerminalStatus_NeverStalls_EvenAfterLongInactivity(CaseStatus status)
    {
        var result = CaseActivityCalculator.ComputeStallingStatus(status, 365);
        Assert.Equal(CaseStallingStatus.None, result);
    }

    [Theory]
    [InlineData(CaseStatus.Active)]
    [InlineData(CaseStatus.OnHold)]
    [InlineData(CaseStatus.Pending)]
    public void ComputeStallingStatus_NonTerminalStatus_CanStall(CaseStatus status)
    {
        var result = CaseActivityCalculator.ComputeStallingStatus(status, 30);
        Assert.Equal(CaseStallingStatus.SevereStalling, result);
    }

    // --- ComputeLastMeaningfulLawyerActivity / full-entity overload ---

    [Fact]
    public void ComputeStallingStatus_FullOverload_CustomerUpdate_DoesNotResetLawyerTimer()
    {
        var lawyerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var caseEntity = new Case { Status = CaseStatus.Active, CreatedAt = now.AddDays(-40) };
        var updates = new List<CaseUpdate>
        {
            new() { CreatedByUserId = customerUserId, CreatedAt = now.AddDays(-1) } // recent, but customer-authored
        };

        var status = CaseActivityCalculator.ComputeStallingStatus(
            caseEntity, updates, [], [], lawyerUserId, now);

        // Only the case creation date (40 days ago) counts as lawyer activity here.
        Assert.Equal(CaseStallingStatus.SevereStalling, status);
    }

    [Fact]
    public void ComputeStallingStatus_FullOverload_LawyerUpdate_ResetsTimer()
    {
        var lawyerUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var caseEntity = new Case { Status = CaseStatus.Active, CreatedAt = now.AddDays(-40) };
        var updates = new List<CaseUpdate>
        {
            new() { CreatedByUserId = lawyerUserId, CreatedAt = now.AddDays(-1) }
        };

        var status = CaseActivityCalculator.ComputeStallingStatus(
            caseEntity, updates, [], [], lawyerUserId, now);

        Assert.Equal(CaseStallingStatus.None, status);
    }

    [Fact]
    public void ComputeActivitySignals_PopulatesStallingFields_ConsistentWithStandaloneOverload()
    {
        var lawyerUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var caseEntity = new Case { Status = CaseStatus.Active, CreatedAt = now.AddDays(-20) };

        var signals = CaseActivityCalculator.ComputeActivitySignals(caseEntity, [], [], [], lawyerUserId, now);

        Assert.Equal(20, signals.DaysSinceLastMeaningfulLawyerActivity);
        Assert.Equal(nameof(CaseStallingStatus.Stalling), signals.StallingStatus);
    }

    // --- ComputeLawyerStallingDeduction ---

    [Fact]
    public void ComputeLawyerStallingDeduction_NoStalling_ReturnsZero()
    {
        var (stallingCount, severeCount, deduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction(
            [CaseStallingStatus.None, CaseStallingStatus.Warning, CaseStallingStatus.None]);

        Assert.Equal(0, stallingCount);
        Assert.Equal(0, severeCount);
        Assert.Equal(0, deduction);
    }

    [Fact]
    public void ComputeLawyerStallingDeduction_OneLongRunningLegitimateCase_DoesNotDestroyScore()
    {
        // A single Warning-level case (< 14 days) must contribute nothing.
        var (stallingCount, severeCount, deduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction(
            [CaseStallingStatus.Warning]);

        Assert.Equal(0, stallingCount);
        Assert.Equal(0, severeCount);
        Assert.Equal(0, deduction);
    }

    [Fact]
    public void ComputeLawyerStallingDeduction_RepeatedStallingAcrossMultipleCases_CompoundsLinearly()
    {
        var (stallingCount, severeCount, deduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction(
            [CaseStallingStatus.Stalling, CaseStallingStatus.Stalling, CaseStallingStatus.SevereStalling]);

        Assert.Equal(2, stallingCount);
        Assert.Equal(1, severeCount);
        Assert.Equal(9, deduction); // 2*2 + 1*5
    }

    [Fact]
    public void ComputeLawyerStallingDeduction_SevereDominates_HeavierThanPlainStalling()
    {
        var (_, _, severeDeduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction([CaseStallingStatus.SevereStalling]);
        var (_, _, stallingDeduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction([CaseStallingStatus.Stalling]);

        Assert.True(severeDeduction > stallingDeduction);
    }
}
