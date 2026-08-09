using LawConnect.Application.Common.Models;

namespace LawConnect.Tests.Unit;

public sealed class PaginationRequestTests
{
    [Fact]
    public void PageSize_IsCappedAtMax()
    {
        var request = new PaginationRequest { PageSize = 500 };

        Assert.Equal(PaginationRequest.MaxPageSize, request.PageSize);
    }

    [Fact]
    public void Skip_UsesPageNumberAndSize()
    {
        var request = new PaginationRequest { PageNumber = 3, PageSize = 10 };

        Assert.Equal(20, request.Skip);
    }
}
