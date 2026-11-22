using Pacer.Engine;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAJourney;

public partial class WhenRunningAJourney
{
    public class AndTheUserIsStopped : JourneyRunnerTestBase
    {
        [Test]
        public async Task ThenAJourneyStoppedBeforeItStartsIsMarkedCancelled()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var outcome = await JourneyRunner.RunAsync([OkStep("login")], this.Context, this.TimeProvider, this.Recorder, cts.Token);

            Assert.That(outcome.WasCancelled, Is.True);
            Assert.That(this.Recorder.Records, Is.Empty);
        }

        [Test]
        public async Task ThenTheInterruptedStepIsNotRecorded()
        {
            using var cts = new CancellationTokenSource();
            var steps = new[] { OkStep("login"), CancellingStep("browse", cts, () => throw new OperationCanceledException(cts.Token)) };

            var outcome = await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, cts.Token);

            Assert.That(outcome.WasCancelled, Is.True);
            Assert.That(this.Recorder.Records.Select(r => r.Name), Is.EqualTo(new[] { "login" }));
        }

        [Test]
        public async Task ThenAStepThatFailsAfterBeingStoppedIsNotRecorded()
        {
            using var cts = new CancellationTokenSource();
            var steps = new[] { CancellingStep("browse", cts, () => StepResult.Fail(status: "cancelled")) };

            var outcome = await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, cts.Token);

            Assert.That(outcome.WasCancelled, Is.True);
            Assert.That(this.Recorder.Records, Is.Empty);
        }

        [Test]
        public async Task ThenAStepThatThrowsAfterBeingStoppedIsNotRecorded()
        {
            using var cts = new CancellationTokenSource();
            var steps = new[] { CancellingStep("browse", cts, () => throw new ObjectDisposedException("client")) };

            var outcome = await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, cts.Token);

            Assert.That(outcome.WasCancelled, Is.True);
            Assert.That(this.Recorder.Records, Is.Empty);
        }

        [Test]
        public async Task ThenAFailureWithoutCancellationIsNotMarkedCancelled()
        {
            var outcome = await JourneyRunner.RunAsync([FailStep("login")], this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(outcome.WasCancelled, Is.False);
            Assert.That(outcome.IsOk, Is.False);
        }

        private static Step CancellingStep(string name, CancellationTokenSource cts, Func<StepResult> then)
        {
            return new Step(name, (_, _) =>
            {
                cts.Cancel();
                return ValueTask.FromResult(then());
            });
        }
    }
}
