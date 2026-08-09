using LawConnect.Domain.Enums;
using LawConnect.Shared.Options;

namespace LawConnect.Tests.Unit.Emergency;

public sealed class EmergencyOptionsTests
{
    [Fact]
    public void Scopes_AreCityStateCountry_UnlessInternationalEnabled()
    {
        var options = new EmergencyOptions { AllowInternational = false };
        Assert.Equal([1, 2, 3], options.GetScopeNumbers());
        Assert.Equal(1, options.ScopeNumberForAttempt(1));
        Assert.Equal(2, options.ScopeNumberForAttempt(2));
        Assert.Equal(3, options.ScopeNumberForAttempt(3));
        Assert.Equal(3, options.ScopeNumberForAttempt(9));
        Assert.True(options.HasNextScope(1));
        Assert.True(options.HasNextScope(2));
        Assert.False(options.HasNextScope(3));

        options.AllowInternational = true;
        Assert.Equal(4, options.ScopeNumberForAttempt(4));
        Assert.True(options.HasNextScope(3));
        Assert.False(options.HasNextScope(4));
    }

    [Fact]
    public void Lifecycle_RejectsInvalidTransitions()
    {
        Assert.True(EmergencyLifecycle.CanAccept(EmergencyStatus.Notifying));
        Assert.True(EmergencyLifecycle.CanAccept(EmergencyStatus.Searching));
        Assert.False(EmergencyLifecycle.CanAccept(EmergencyStatus.Cancelled));
        Assert.False(EmergencyLifecycle.CanAccept(EmergencyStatus.Assigned));
        Assert.False(EmergencyLifecycle.CanCancel(EmergencyStatus.Completed));
        Assert.True(EmergencyLifecycle.CanCreateCall(EmergencyStatus.Assigned));
        Assert.False(EmergencyLifecycle.CanCreateCall(EmergencyStatus.Searching));
        Assert.True(EmergencyLifecycle.CanMarkActive(EmergencyStatus.Calling));
        Assert.False(EmergencyLifecycle.IsOpen(EmergencyStatus.Expired));
        Assert.True(EmergencyLifecycle.IsSearching(EmergencyStatus.Searching));
    }
}
