namespace Usenet.Tests.TestHelpers;

/// <summary>
/// Helpers for measuring managed-heap allocations so the suite can guard the rebuilt
/// hot paths against allocation regressions. Buffers rented from <see cref="System.Buffers.ArrayPool{T}"/>
/// are deliberately not counted here: the byte-oriented paths (ADR-0002) rent their working
/// buffers, so the managed allocation that remains is the per-call object/string churn.
/// </summary>
internal static class AllocationMeasurement
{
    /// <summary>
    /// Runs a synchronous action <paramref name="iterations"/> times on the calling thread and
    /// returns the average managed bytes allocated per iteration. The action is run once first to
    /// pay one-off JIT and static-init costs, then the heap is settled before measuring.
    /// </summary>
    public static long PerIteration(Action action, int iterations)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

        // Warm up so first-call JIT and one-off allocations don't land in the measured window.
        action();
        Settle();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
        {
            action();
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        return (after - before) / iterations;
    }

    /// <summary>
    /// Measures the total managed bytes allocated across all threads while awaiting
    /// <paramref name="action"/>. Uses the process-wide counter because async work can hop
    /// threads, which the per-thread counter cannot follow. Intended to be paired with a
    /// marginal (delta-between-sizes) measurement so fixed per-call overhead cancels out.
    /// </summary>
    public static async Task<long> TotalAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Settle();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        await action();
        var after = GC.GetTotalAllocatedBytes(precise: true);

        return after - before;
    }

    private static void Settle()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
