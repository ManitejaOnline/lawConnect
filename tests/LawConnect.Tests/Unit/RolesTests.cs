using LawConnect.Shared.Constants;

namespace LawConnect.Tests.Unit;

public sealed class RolesTests
{
    [Fact]
    public void Roles_All_ContainsExpectedValues()
    {
        Assert.Contains(Roles.Customer, Roles.All);
        Assert.Contains(Roles.Lawyer, Roles.All);
        Assert.Contains(Roles.Admin, Roles.All);
        Assert.Equal(3, Roles.All.Count);
    }
}
