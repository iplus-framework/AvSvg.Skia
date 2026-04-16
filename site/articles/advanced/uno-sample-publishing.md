---
title: "Uno Sample Publishing"
---

# Uno Sample Publishing

The Uno sample app lives at `samples/UnoSvgSkiaSample` and stays out of `Svg.Skia.slnx`, so the default repository build and test workflow remains workload-free.

## Prerequisite workloads

```bash
uno-check --target desktop --target web --target android --target ios
```

## Desktop

```bash
dotnet build samples/UnoSvgSkiaSample/UnoSvgSkiaSample.csproj -c Release -f net10.0-desktop
```

## WebAssembly

```bash
dotnet publish samples/UnoSvgSkiaSample/UnoSvgSkiaSample.csproj -c Release -f net10.0-browserwasm
```

## Android

```bash
dotnet build samples/UnoSvgSkiaSample/UnoSvgSkiaSample.csproj -c Release -f net10.0-android
```

## iOS on macOS

```bash
dotnet build samples/UnoSvgSkiaSample/UnoSvgSkiaSample.csproj -c Release -f net10.0-ios
```

## What to verify manually

- asset loading through `Path`
- inline SVG through `Source`
- shared `SvgSource` resource usage
- runtime CSS switching
- zoom, pan, and hit testing
- wireframe and filter toggle behavior
