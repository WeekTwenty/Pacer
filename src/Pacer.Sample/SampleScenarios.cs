using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Sample;

/// <summary>Shared data prepared once before the checkout scenario runs.</summary>
public sealed record CheckoutData(string AuthToken, IReadOnlyList<int> ProductIds);

/// <summary>
/// Example scenarios that simulate a small web workload. The steps just sleep for a randomised,
/// realistic amount of time and occasionally fail, so the sample runs anywhere with no external
/// dependencies while still producing meaningful latency and throughput numbers. Their load profile
/// lives in <c>appsettings.json</c> under <c>Pacer:Load</c>, not in code.
/// </summary>
public static class SampleScenarios
{
    /// <summary>A three-step checkout journey with inline one-time setup.</summary>
    public static Scenario Checkout() => Scenario.Create("checkout")
        .InGroup("storefront")
        .WithSetup(ctx =>
        {
            ctx.Logger.LogInformation("Preparing checkout test data…");
            var products = Enumerable.Range(1000, 50).ToArray();
            return ValueTask.FromResult(new CheckoutData($"token-{Guid.NewGuid():N}", products));
        })
        .WithWarmup(TimeSpan.FromSeconds(2))
        .AddStep("login", async ctx =>
        {
            var data = ctx.ScenarioDataAs<CheckoutData>()!;
            await SimulateWorkAsync(ctx, 5, 15);
            return StepResult.Ok(bytesSent: 128, bytesReceived: 256, payload: data.AuthToken);
        })
        .AddStep("browse", async ctx =>
        {
            var data = ctx.ScenarioDataAs<CheckoutData>()!;
            var product = data.ProductIds[ctx.Random.Next(data.ProductIds.Count)];
            await SimulateWorkAsync(ctx, 10, 35);
            return StepResult.Ok(bytesReceived: 4096, payload: product);
        })
        .AddStep("purchase", async ctx =>
        {
            // The product chosen by "browse" arrives as the previous step's payload.
            var product = ctx.PreviousAs<int>();
            if (product == 0)
                return StepResult.Fail(status: "no-product");

            await SimulateWorkAsync(ctx, 15, 45);
            return ctx.Random.NextDouble() < 0.02
                ? StepResult.Fail(status: "payment-declined")
                : StepResult.Ok(bytesSent: 320, bytesReceived: 512, payload: product);
        });

    /// <summary>
    /// An order journey against <see cref="FakeStoreApi"/>, showing the features a realistic test
    /// leans on: dependencies resolved from <c>ctx.Services</c>, setup data shared by every virtual
    /// user, each step's result handed to the next via <c>ctx.PreviousAs&lt;T&gt;()</c>, per-user
    /// context (<c>VirtualUserId</c>, <c>InvocationNumber</c>), and a teardown that cleans up.
    /// </summary>
    public static Scenario Orders() => Scenario.Create("orders")
        .InGroup("account")
        // Setup runs once: resolve the API client from DI and seed a customer for every user to share.
        .WithSetup(async (ISetupContext ctx, CancellationToken cancellationToken) =>
        {
            var api = ctx.Services.GetRequiredService<FakeStoreApi>();
            var customer = await api.CreateCustomerAsync(cancellationToken);
            ctx.Logger.LogInformation("Seeded customer {Customer} for '{Scenario}'.", customer.Id, ctx.ScenarioName);
            return customer;
        })
        // Steps that take the cancellation token directly use the (ctx, cancellationToken) overload.
        .AddStep("create-cart", async (ctx, cancellationToken) =>
        {
            var api = ctx.Services.GetRequiredService<FakeStoreApi>();
            var customer = ctx.ScenarioDataAs<Customer>()!;
            var cart = await api.CreateCartAsync(customer, ctx.VirtualUserId, ctx.Random, cancellationToken);
            return StepResult.Ok(bytesSent: 96, bytesReceived: 160, payload: cart);
        })
        .AddStep("add-items", async (ctx, cancellationToken) =>
        {
            var api = ctx.Services.GetRequiredService<FakeStoreApi>();
            var cart = ctx.PreviousAs<Cart>()!;

            // Every tenth journey a user makes is a big basket; the rest add one to three items.
            var count = ctx.InvocationNumber % 10 == 0 ? 8 : ctx.Random.Next(1, 4);
            for (var i = 0; i < count; i++)
            {
                cart = await api.AddItemAsync(cart, 1000 + ctx.Random.Next(50), ctx.Random, cancellationToken);
                ctx.AddBytesSent(48);
            }

            ctx.AddBytesReceived(64L * cart.Items.Count);
            return StepResult.Ok(payload: cart);
        })
        .AddStep("place-order", async (ctx, cancellationToken) =>
        {
            var api = ctx.Services.GetRequiredService<FakeStoreApi>();
            var cart = ctx.PreviousAs<Cart>()!;

            var order = await api.PlaceOrderAsync(cart, ctx.Random, cancellationToken);
            return order is null
                ? StepResult.Fail(status: "out-of-stock")
                : StepResult.Ok(bytesSent: 128, bytesReceived: 256, payload: order);
        })
        // Teardown runs once after the load phase and sees what setup returned, so it removes exactly
        // the customer it seeded (and that customer's carts and orders).
        .WithTeardown(async (ctx, cancellationToken) =>
        {
            var api = ctx.Services.GetRequiredService<FakeStoreApi>();
            var customer = ctx.ScenarioDataAs<Customer>()!;
            var removed = await api.DeleteCustomerAsync(customer, cancellationToken);
            ctx.Logger.LogInformation("Removed customer {Customer} and {Count} related records.", customer.Id, removed);
        });

    /// <summary>A simpler two-step search scenario sharing the storefront group.</summary>
    public static Scenario Search() => Scenario.Create("search")
        .InGroup("storefront")
        // These steps report transfer via the context byte counters rather than on the result —
        // handy when a step makes several calls, or you want the count recorded even on failure.
        .AddStep("query", async ctx =>
        {
            await SimulateWorkAsync(ctx, 8, 25);
            ctx.AddBytesReceived(8192);
            return StepResult.Ok();
        })
        .AddStep("render", async ctx =>
        {
            await SimulateWorkAsync(ctx, 3, 12);
            ctx.AddBytesReceived(1024);
            return StepResult.Ok();
        });

    private static Task SimulateWorkAsync(IStepContext context, int minMs, int maxMs)
        => Task.Delay(context.Random.Next(minMs, maxMs), context.CancellationToken);
}
