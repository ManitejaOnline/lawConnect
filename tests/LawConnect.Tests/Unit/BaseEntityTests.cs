using LawConnect.Domain.Common;

namespace LawConnect.Tests.Unit;

public sealed class BaseEntityTests
{
    [Fact]
    public void BaseEntity_NewInstance_HasNonEmptyId()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void BaseEntity_Defaults_AreNotSoftDeleted()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    private sealed class TestEntity : BaseEntity;
}
