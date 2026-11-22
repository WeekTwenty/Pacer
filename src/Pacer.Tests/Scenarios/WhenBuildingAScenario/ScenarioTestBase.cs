using Pacer.Steps;

namespace Pacer.Tests.Scenarios.WhenBuildingAScenario;

public class ScenarioTestBase
{
    protected static StepDelegate AnyStep()
    {
        return (_, _) => ValueTask.FromResult(StepResult.Ok());
    }
}