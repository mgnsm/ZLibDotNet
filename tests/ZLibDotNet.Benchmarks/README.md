### How to run the benchmarks?

- Install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Clone this repository: `git clone https://github.com/mgnsm/ZLibDotNet.git`
- Run the [dotnet run](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run) command: `dotnet run --project tests/ZLibDotNet.Benchmarks -c Release -f net8.0 -- -f *`

 If you don't want to run all benchmarks, you can filter some of them by replacing the `*` with a string that matches a specific class or method.
 
 For example, the following command runs the benchmarks defined in the [InflateBenchmarks](InflateBenchmarks.cs) class only:
    
     dotnet run --project tests/ZLibDotNet.Benchmarks -c Release -f net8.0 -- -f *.Inflate*

Please refer to [BenchmarkDotNet.org](https://benchmarkdotnet.org/index.html) to learn more about how to configure and run the benchmarks.

The results of running the  deflate (compression) and inflate (uncompression) benchmarks on a development machine can be found [here](DeflateBenchmarks-report.md) and [here](InflateBenchmarks-report.md) respectively.