using BenchmarkDotNet.Running;

namespace Usenet.Benchmarks;

/// <summary>
/// Entry point for the BenchmarkDotNet harness. Run all benchmarks with
/// <c>dotnet run -c Release -f net10.0 -- --filter *</c> or pick individual
/// benchmarks via the interactive switcher.
/// </summary>
internal static class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
