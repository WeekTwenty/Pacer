namespace Pacer.Tests.History;

/// <summary>Provides a fresh, isolated temp directory for store tests and cleans it up afterwards.</summary>
public class StoreTestBase
{
    protected string DataDirectory { get; private set; } = null!;

    [SetUp]
    public void SetUp()
        => DataDirectory = Path.Combine(Path.GetTempPath(), "pacer-store-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(DataDirectory))
            Directory.Delete(DataDirectory, recursive: true);
    }
}
