### How to run the benchmarks?

- Install the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- Clone this repository: `git clone https://github.com/mgnsm/ZLibDotNet.git`
- Run the [dotnet run](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run) command: `dotnet run --project tests/ZLibDotNet.Benchmarks -c Release -f net6.0 -- -f *`

Please refer to [BenchmarkDotNet.org](https://benchmarkdotnet.org/index.html) to learn more about how to configure and run the benchmarks.