using System.Collections.Concurrent;

namespace Pacer.Sample;

/// <summary>A customer seeded once by the orders scenario's setup.</summary>
public sealed record Customer(Guid Id, string Name);

/// <summary>A shopping cart owned by one virtual user.</summary>
public sealed record Cart(Guid Id, Guid CustomerId, int VirtualUserId, IReadOnlyList<int> Items);

/// <summary>A placed order.</summary>
public sealed record Order(Guid Id, Guid CartId, decimal Total);

/// <summary>
/// An in-memory stand-in for the HTTP client a real load test would call. It is registered in
/// dependency injection (see <c>Program.cs</c>) and resolved from <c>ctx.Services</c> in setup,
/// steps and teardown, which is how you would use a typed <c>HttpClient</c>. Each call sleeps for a
/// realistic, randomised time so the sample produces meaningful latencies without a network.
/// </summary>
public sealed class FakeStoreApi
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();
    private readonly ConcurrentDictionary<Guid, Cart> _carts = new();
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly ConcurrentDictionary<Guid, Guid> _cartOwners = new();

    /// <summary>Creates a customer (setup-time; called once per run).</summary>
    public async Task<Customer> CreateCustomerAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        var customer = new Customer(Guid.NewGuid(), "load-test-customer");
        _customers[customer.Id] = customer;
        return customer;
    }

    /// <summary>Opens an empty cart for a virtual user.</summary>
    public async Task<Cart> CreateCartAsync(Customer customer, int virtualUserId, Random random, CancellationToken cancellationToken)
    {
        await Task.Delay(random.Next(4, 12), cancellationToken);
        var cart = new Cart(Guid.NewGuid(), customer.Id, virtualUserId, []);
        _carts[cart.Id] = cart;
        _cartOwners[cart.Id] = customer.Id;
        return cart;
    }

    /// <summary>Adds a product to a cart and returns the updated cart.</summary>
    public async Task<Cart> AddItemAsync(Cart cart, int productId, Random random, CancellationToken cancellationToken)
    {
        await Task.Delay(random.Next(2, 8), cancellationToken);
        var updated = cart with { Items = [.. cart.Items, productId] };
        _carts[cart.Id] = updated;
        return updated;
    }

    /// <summary>Places an order for a cart, or returns <see langword="null"/> when an item is out of stock.</summary>
    public async Task<Order?> PlaceOrderAsync(Cart cart, Random random, CancellationToken cancellationToken)
    {
        await Task.Delay(random.Next(12, 40), cancellationToken);
        if (random.NextDouble() < 0.01)
            return null;

        var order = new Order(Guid.NewGuid(), cart.Id, cart.Items.Count * 9.99m);
        _orders[order.Id] = order;
        _carts.TryRemove(cart.Id, out _);
        return order;
    }

    /// <summary>
    /// Deletes a customer with its carts and orders (teardown-time) and returns how many related
    /// carts and orders were removed.
    /// </summary>
    public async Task<int> DeleteCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);

        var carts = _carts.Values.Where(c => c.CustomerId == customer.Id).Select(c => c.Id).ToHashSet();
        var removed = 0;
        foreach (var order in _orders.Values.Where(o => _cartOwners.TryGetValue(o.CartId, out var owner) && owner == customer.Id))
            removed += _orders.TryRemove(order.Id, out _) ? 1 : 0;
        foreach (var cartId in carts)
            removed += _carts.TryRemove(cartId, out _) ? 1 : 0;

        foreach (var owned in _cartOwners.Where(o => o.Value == customer.Id))
            _cartOwners.TryRemove(owned.Key, out _);

        _customers.TryRemove(customer.Id, out _);
        return removed;
    }
}
