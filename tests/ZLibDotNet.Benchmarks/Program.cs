global using static ZLibDotNet.ZLib;
using BenchmarkDotNet.Running;
using System.Reflection;

[assembly: CLSCompliant(false)]

#pragma warning disable CA1812 //https://github.com/dotnet/roslyn-analyzers/issues/5628
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
#pragma warning restore CA1812