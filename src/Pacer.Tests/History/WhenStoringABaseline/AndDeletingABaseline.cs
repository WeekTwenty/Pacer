using Pacer.History;

namespace Pacer.Tests.History.WhenStoringABaseline;

public partial class WhenStoringABaseline
{
    public class AndDeletingABaseline : StoreTestBase
    {
        [Test]
        public async Task ThenItRemovesOnlyTheNamedBaseline()
        {
            var store = new JsonBaselineStore();
            await store.SaveAsync(DataDirectory, "before", HistoryFixtures.Record("checkout"));
            await store.SaveAsync(DataDirectory, "after", HistoryFixtures.Record("checkout"));

            await store.DeleteAsync(DataDirectory, "checkout", "before");

            Assert.That(await store.LoadAsync(DataDirectory, "checkout", "before"), Is.Null);
            Assert.That(await store.LoadAsync(DataDirectory, "checkout", "after"), Is.Not.Null);
        }

        [Test]
        public async Task ThenDeletingAMissingBaselineIsANoOp()
        {
            var store = new JsonBaselineStore();

            await store.DeleteAsync(DataDirectory, "missing", "default");

            Assert.That(await store.LoadAsync(DataDirectory, "missing", "default"), Is.Null);
        }

        [Test]
        public async Task ThenItListsScenariosWithSavedBaselines()
        {
            var store = new JsonBaselineStore();
            await store.SaveAsync(DataDirectory, "default", HistoryFixtures.Record("checkout"));
            await store.SaveAsync(DataDirectory, "default", HistoryFixtures.Record("search"));

            Assert.That(await store.ListScenariosAsync(DataDirectory), Is.EqualTo(new[] { "checkout", "search" }));
        }
    }
}
