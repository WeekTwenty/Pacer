using Pacer.History;

namespace Pacer.Tests.History.WhenAppendingHistory;

public partial class WhenAppendingHistory
{
    public class AndAppendingARun : StoreTestBase
    {
        [Test]
        public async Task ThenItCanBeReadBack()
        {
            var store = new JsonHistoryStore();
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("checkout"));

            var records = await store.LoadAsync(DataDirectory, "checkout");

            Assert.That(records, Has.Count.EqualTo(1));
            Assert.That(records[0].ScenarioName, Is.EqualTo("checkout"));
        }

        [Test]
        public async Task ThenItReturnsRunsInInsertionOrder()
        {
            var store = new JsonHistoryStore();
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("checkout"));
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("checkout"));

            var records = await store.LoadAsync(DataDirectory, "checkout");

            Assert.That(records, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task ThenItReturnsEmptyForAnUnknownScenario()
        {
            var store = new JsonHistoryStore();

            var records = await store.LoadAsync(DataDirectory, "missing");

            Assert.That(records, Is.Empty);
        }

        [Test]
        public async Task ThenItRoundTripsLatencyAndEnvironment()
        {
            var store = new JsonHistoryStore();
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("checkout"));

            var record = (await store.LoadAsync(DataDirectory, "checkout"))[0];

            Assert.That(record.Journey.P99Ms, Is.EqualTo(180));
            Assert.That(record.Environment.MachineName, Is.EqualTo("test-machine"));
        }
    }
}
