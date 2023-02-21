### How to run the benchmarks?

- Install the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- Clone this repository: `git clone https://github.com/mgnsm/ZLibDotNet.git`
- Run the [dotnet run](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run) command: `dotnet run --project tests/ZLibDotNet.Benchmarks -c Release -f net6.0 -- -f *`

 If you don't want to run all benchmarks, you can filter some of them by replacing the `*` with a string that matches a specific class or method.
 
 For example, the following command runs the benchmarks defined in the [UncompressionBenchmarks](UncompressionBenchmarks.cs) class only:
    
     dotnet run --project tests/ZLibDotNet.Benchmarks -c Release -f net6.0 -- -f *.Uncompress*

Please refer to [BenchmarkDotNet.org](https://benchmarkdotnet.org/index.html) to learn more about how to configure and run the benchmarks.

The results of running the compression and uncompression benchmarks on a development machine can be found [here](CompressionBenchmarks-report.md) and [here](UncompressionBenchmarks-report.md) respectively.