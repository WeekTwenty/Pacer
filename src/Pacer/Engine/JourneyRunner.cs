using Microsoft.Extensions.Logging;
using Pacer.Steps;

namespace Pacer.Engine;

/// <summary>
/// The result of running one journey (a single pass through a scenario's steps).
/// <see cref="WasCancelled"/> is set when the virtual user was stopped before the journey finished;
/// such a journey is neither a success nor a failure.
/// </summary>
internal readonly record struct JourneyOutcome(bool IsOk, TimeSpan Elapsed, long BytesSent, long BytesReceived, bool WasCancelled = false);

/// <summary>
/// Executes a scenario's steps once, in order, as a pipeline for a single virtual user. Each step
/// is timed and recorded; a failed or throwing step ends the journey early; a successful step's
/// payload is handed to the next step via <see cref="IStepContext.Previous"/>. If the token is
/// cancelled mid-journey, the interrupted step is not recorded and the outcome is marked cancelled.
/// </summary>
internal static class JourneyRunner
{
    public static async ValueTask<JourneyOutcome> RunAsync(
        IReadOnlyList<Step> steps,
        StepContext context,
        TimeProvider timeProvider,
        IStepRecorder recorder,
        CancellationToken cancellationToken)
    {
        context.Previous = null;
        var journeyStart = timeProvider.GetTimestamp();
        var allOk = true;
        var cancelled = false;
        long bytesSent = 0, bytesReceived = 0;

        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                allOk = false;
                cancelled = true;
                break;
            }

            var stepStart = timeProvider.GetTimestamp();
            StepResult result;
            try
            {
                result = await step.Execute(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                allOk = false;
                cancelled = true;
                break;
            }
            catch (Exception ex)
            {
                result = StepResult.Fail(ex.GetType().Name);
                if (!cancellationToken.IsCancellationRequested)
                    context.Logger.LogError(ex, "Step '{Step}' threw an exception.", step.Name);
            }

            var elapsed = timeProvider.GetElapsedTime(stepStart);

            // A step that failed after the user was told to stop (e.g. it caught the cancellation and
            // returned Fail, or its client threw on shutdown) was interrupted, not broken.
            if (!result.IsOk && cancellationToken.IsCancellationRequested)
            {
                context.TakeBytes();
                allOk = false;
                cancelled = true;
                break;
            }

            // Fold any bytes the step accumulated via the context counters into the result, on top of
            // whatever it reported on the StepResult itself, then drain the counters for the next step.
            var (countedSent, countedReceived) = context.TakeBytes();
            result = result.WithBytes(result.BytesSent + countedSent, result.BytesReceived + countedReceived);

            recorder.Record(step.Name, elapsed, result);
            bytesSent += result.BytesSent;
            bytesReceived += result.BytesReceived;

            if (!result.IsOk)
            {
                allOk = false;
                break;
            }

            context.Previous = result.Payload;
        }

        return new JourneyOutcome(allOk, timeProvider.GetElapsedTime(journeyStart), bytesSent, bytesReceived, cancelled);
    }
}
