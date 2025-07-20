using System.Text;
using BenchmarkDotNet.Attributes;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;

namespace Identity.Benchmarks;

/// <summary>
/// Benchmarks the performance of the Argon2 password hashing algorithm
/// with varying configurations for iterations, memory cost, and parallelism.
/// </summary>
[MemoryDiagnoser]
public class Argon2Bench {
    /// <summary>
    /// Example password to hash during benchmarking.
    /// </summary>
    private const string Password = "P@ssw0rd!";

    /// <summary>
    /// Byte array representation of the password.
    /// </summary>
    private byte[] _passwordBytes = []; // Initialize to avoid nullability issues

    /// <summary>
    /// Number of iterations for Argon2 hashing (affects computation time).
    /// </summary>
    [Params(1, 2, 3, 4)] // Expanded range for iterations
    public int Iterations;

    /// <summary>
    /// Memory cost for Argon2 hashing (affects memory usage).
    /// </summary>
    [Params(32768, 65536, 131072, 262144)] // Expanded range for memory cost
    public int MemoryCost;

    /// <summary>
    /// Number of threads used for parallelism in Argon2 hashing.
    /// </summary>
    [Params(1, 2, 4)] // Expanded range for parallelism
    public int Parallelism;

    /// <summary>
    /// Prepares the password bytes before benchmarking.
    /// </summary>
    [GlobalSetup]
    public void Setup() => _passwordBytes = Encoding.UTF8.GetBytes(Password);

    /// <summary>
    /// Benchmarks the Argon2 hashing process with the configured parameters.
    /// </summary>
    [Benchmark]
    public void HashPassword() {
        // Configures the Argon2 hashing algorithm
        var config = new Argon2Config {
            Type = Argon2Type.DataDependentAddressing, // Argon2d variant
            Version = Argon2Version.Nineteen, // Latest Argon2 version
            TimeCost = Iterations, // Number of iterations
            MemoryCost = MemoryCost, // Memory usage in KB
            Lanes = Parallelism, // Number of parallel lanes
            Threads = Parallelism, // Number of threads
            Password = _passwordBytes, // Password to hash
            Salt = Encoding.UTF8.GetBytes("somesalt") // Example salt (not for production)
        };

        // Performs the hashing and manages resources
        using var argon2 = new Argon2(config); // Simplified using statement
        using SecureArray<byte> hash = argon2.Hash(); // Hash result stored in a secure array
        byte[] hashBytes = hash.Buffer; // Access the underlying byte array (for demonstration)
    }
}
