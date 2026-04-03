# Svg.Skia Benchmarks

This harness measures the shared SVG animation hot path in `Svg.Skia`.

## Included scenarios

- layered top-level animation updates that reuse cached static content
- defs-backed animation updates that still fall back to full-document rebuilds
- the same two scenarios with a draw pass included

## Run

```bash
dotnet run -c Release --project tests/Svg.Skia.Benchmarks/Svg.Skia.Benchmarks.csproj -- --filter "*SvgAnimationFrameBenchmarks*"
```

The benchmark project uses a short-run BenchmarkDotNet job by default so it can be used as a local comparison harness while iterating on the animation renderer.
