# Identity.Benchmarks

## Overview
This project benchmarks the performance of the Argon2 password hashing algorithm using various configurations of its parameters. The goal is to tune the algorithm for optimal security and performance, balancing computational cost and memory usage.

## Key Parameters of Argon2
Argon2 is a memory-hard password hashing algorithm with the following tunable parameters:

1. **Iterations (`TimeCost`)**:
   - The number of times the hashing process is repeated.
   - Higher values increase computational cost, improving security but slowing down legitimate operations.

2. **Memory Cost (`MemoryCost`)**:
   - The amount of memory (in kilobytes) used during hashing.
   - Higher values make the algorithm more resistant to GPU-based attacks but require more system resources.

3. **Parallelism (`Lanes` and `Threads`)**:
   - The number of threads or lanes used for hashing in parallel.
   - Higher values improve performance on multi-core systems but increase resource usage.

## Benchmark Results Analysis
The benchmark results provide insights into how different configurations of `Iterations`, `MemoryCost`, and `Parallelism` affect performance and resource usage. Below is a summary of the findings:

### Observations
1. **Performance (Mean Time)**:
   - Increasing `Iterations` or `MemoryCost` significantly increases the hashing time.
   - Higher `Parallelism` reduces hashing time, especially for higher `MemoryCost` values, by utilizing multiple threads.

2. **Memory Usage (Allocated)**:
   - Memory usage scales linearly with `MemoryCost`.
   - Higher `Parallelism` slightly increases memory usage due to thread overhead.

3. **Trade-offs**:
   - Higher `Iterations` and `MemoryCost` improve security but increase computational and memory costs.
   - Higher `Parallelism` improves performance but requires more system resources.

### Example Configurations
- **Low-Security Environment** (e.g., development):
  - `Iterations = 1`, `MemoryCost = 32,768`, `Parallelism = 1`
  - Fast hashing (~45ms) with moderate memory usage (~133MB).

- **High-Security Environment** (e.g., financial systems):
  - `Iterations = 4`, `MemoryCost = 262,144`, `Parallelism = 4`
  - Highly secure but resource-intensive (~1,290ms, ~2.67GB memory).

## Tuning Parameters in Production
Based on the benchmark results, the following tuning parameters were chosen and stored in `appsettings.json`:

```json
"Argon2": {
    "Tuning": [
        { "MemoryCost": 32768, "Iterations": 2 },
        { "MemoryCost": 65536, "Iterations": 3 },
        { "MemoryCost": 131072, "Iterations": 4 },
        { "MemoryCost": 262144, "Iterations": 5 }
    ]
}
```

### Reasoning
1. **MemoryCost**:
   - Values were chosen to represent a reasonable progression of memory usage (128MB, 256MB, 512MB, and 1GB+).
   - These values are practical for modern systems while still providing strong security.

2. **Iterations**:
   - Values were increased proportionally with `MemoryCost` to ensure the hashing process remains computationally expensive.

3. **Parallelism**:
   - Not included in the tuning parameters as it is highly hardware-dependent and can be set dynamically based on the runtime environment.

## How to Run the Benchmark
1. **Build the Project**:
   ```sh
   dotnet build
   ```

2. **Run the Benchmark**:
   ```sh
   dotnet run -c Release
   ```

3. **View Results**:
   - Results will be displayed in the console and saved in the `BenchmarkDotNet.Artifacts` folder.

## Conclusion
The benchmark results provide valuable insights into the performance and resource usage of Argon2 configurations. These findings were used to tune the algorithm for production, ensuring a balance between security and performance. The chosen parameters are stored in `appsettings.json` for easy configuration and dynamic adjustment.