using BenchmarkDotNet.Running;

namespace Identity.Benchmarks;

/// <summary>
/// Entry point for running the benchmarks.
/// </summary>
public static class Program {
    public static void Main(string[] _) =>
        // Run the Argon2 benchmark
        BenchmarkRunner.Run<Argon2Bench>();
}