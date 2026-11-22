using Pacer.History;

namespace Pacer.Tests.History.WhenAppendingHistory;

public partial class WhenAppendingHistory
{
    public class AndListingScenarios : StoreTestBase
    {
        [Test]
        public async Task ThenItListsEveryRecordedScenario()
        {
            var store = new JsonHistoryStore();
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("checkout"));
            await store.AppendAsync(DataDirectory, HistoryFixtures.Record("search"));

            var scenarios = await store.ListScenariosAsync(DataDirectory);

            Assert.That(scenarios, Is.EqualTo(new[] { "checkout", "search" }));
        }
    }
}
