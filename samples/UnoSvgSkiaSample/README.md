# UnoSvgSkiaSample

Standalone Uno Platform sample for `Svg.Controls.Skia.Uno`.

## Prerequisites

```bash
uno-check --target desktop --target web --target android --target ios
```

## Build and publish

Desktop:

```bash
dotnet build -c Release -f net10.0-desktop
```

WebAssembly:

```bash
dotnet publish -c Release -f net10.0-browserwasm
```

Android:

```bash
dotnet build -c Release -f net10.0-android
```

iOS on macOS:

```bash
dotnet build -c Release -f net10.0-ios
```

## What the sample covers

- asset loading through `Path`
- inline SVG text through `Source`
- reusable `SvgSource` resources
- runtime CSS restyling
- zoom, pan, and hit testing
- wireframe and filter toggles
