using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Hosting.WhenPlanningARun;

public class PlannerTestBase
{
    protected static ScenarioDefinition Scenario(string name = "checkout", TimeSpan? warmup = null)
    {
        var builder = Pacer.Scenarios.Scenario.Create(name)
            .AddStep("step", (_, _) => ValueTask.FromResult(StepResult.Ok()));
        if (warmup is { } value)
        {
            builder.WithWarmup(value);
        }

        return builder.Build();
    }

    protected static LoadStage Stage(LoadProfile profile, int index)
    {
        return profile.Stages[index];
    }
}
