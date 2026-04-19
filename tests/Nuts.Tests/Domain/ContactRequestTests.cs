using Nuts.Domain.Entities;

namespace Nuts.Tests.Domain;

public class ContactRequestTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = ContactRequest.Create("Ivan", "+79000000000", "i@e.com", "msg");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = ContactRequest.Create("", "+79000000000", null, null);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_WithEmptyPhone_Fails()
    {
        var result = ContactRequest.Create("Ivan", "", null, null);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void MarkProcessed_SetsIsProcessedTrue()
    {
        var request = ContactRequest.Create("Ivan", "+79000000000", null, null).Value;
        request.MarkProcessed();
        Assert.True(request.IsProcessed);
    }
}
