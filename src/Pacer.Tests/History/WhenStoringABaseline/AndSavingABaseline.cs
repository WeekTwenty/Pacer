using Pacer.History;

namespace Pacer.Tests.History.WhenStoringABaseline;

public partial class WhenStoringABaseline
{
    public class AndSavingABaseline : StoreTestBase
    {
        [Test]
        public async Task ThenItCanBeLoadedBack()
        {
            var store = new JsonBaselineStore();
            await store.SaveAsync(DataDirectory, "default", HistoryFixtures.Record("checkout"));

            var baseline = await store.LoadAsync(DataDirectory, "checkout", "default");

            Assert.That(baseline, Is.Not.Null);
            Assert.That(baseline!.ScenarioName, Is.EqualTo("checkout"));
            Assert.That(baseline.Journey.P99Ms, Is.EqualTo(180));
        }

        [Test]
        public async Task ThenItStoresMultipleNamedBaselinesPerScenario()
        {
            var store = new JsonBaselineStore();
            await store.SaveAsync(DataDirectory, "before", HistoryFixtures.Record("checkout"));
            await store.SaveAsync(DataDirectory, "after", HistoryFixtures.Record("checkout"));

            Assert.That(await store.ListBaselinesAsync(DataDirectory, "checkout"), Is.EqualTo(new[] { "after", "before" }));
        }

        [Test]
        public async Task ThenLoadingAnUnknownScenarioOrBaselineReturnsNull()
        {
            var store = new JsonBaselineStore();
            await store.SaveAsync(DataDirectory, "default", HistoryFixtures.Record("checkout"));

            Assert.That(await store.LoadAsync(DataDirectory, "missing", "default"), Is.Null);
            Assert.That(await store.LoadAsync(DataDirectory, "checkout", "missing"), Is.Null);
        }
    }
}
