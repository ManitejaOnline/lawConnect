using LawConnect.Shared.Constants;

namespace LawConnect.Tests.Unit;

public sealed class SeedIdsTests
{
    [Fact]
    public void RoleIds_AreDistinct()
    {
        var ids = new[]
        {
            SeedIds.Roles.Customer,
            SeedIds.Roles.Lawyer,
            SeedIds.Roles.Admin
        };

        Assert.Equal(3, ids.Distinct().Count());
    }
}
