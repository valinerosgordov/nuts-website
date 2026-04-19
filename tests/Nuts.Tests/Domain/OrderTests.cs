using Nuts.Domain.Entities;

namespace Nuts.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Order.Create(Guid.NewGuid(), "Moscow, Lenina 1");

        Assert.True(result.IsSuccess);
        Assert.Equal("Created", result.Value.Status);
    }

    [Fact]
    public void Create_WithEmptyAddress_Fails()
    {
        var result = Order.Create(Guid.NewGuid(), "");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CreateGuest_WithValidData_Succeeds()
    {
        var result = Order.CreateGuest("Ivan", "+79000000001", "i@e.com", "addr", "10-14", null, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateGuest_WithEmptyName_Fails()
    {
        var result = Order.CreateGuest("", "+79000000001", null, "addr", null, null, null);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddItem_WhenStatusIsCreated_Succeeds()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;
        var result = order.AddItem(Guid.NewGuid(), "Walnut", 2, 100m, "500g");

        Assert.True(result.IsSuccess);
        Assert.Single(order.Items);
        Assert.Equal(200m, order.TotalAmount);
    }

    [Fact]
    public void UpdateStatus_FollowsStateMachine()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;

        Assert.True(order.UpdateStatus("Processing").IsSuccess);
        Assert.True(order.UpdateStatus("Shipped").IsSuccess);
        Assert.True(order.UpdateStatus("Delivered").IsSuccess);
    }

    [Fact]
    public void UpdateStatus_InvalidTransition_Fails()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;
        var result = order.UpdateStatus("Delivered");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Delivered_IsTerminal()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;
        order.UpdateStatus("Processing");
        order.UpdateStatus("Shipped");
        order.UpdateStatus("Delivered");

        var result = order.UpdateStatus("Processing");

        Assert.True(result.IsFailure);
    }
}
