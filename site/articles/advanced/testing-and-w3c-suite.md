---
title: "Testing and W3C Suite"
---

# Testing and W3C Suite

Svg.Skia uses several layers of verification.

## Unit tests

The `tests/` folder covers:

- runtime renderer behavior,
- hit testing,
- picture rebuild behavior,
- font and typeface selection,
- Avalonia integration,
- VectorDrawable parsing and validation.

## W3C SVG 1.1 suite

The repository includes the W3C SVG 1.1 test-suite assets as a submodule under `externals/W3C_SVG_11_TestSuite`.

`tests/Svg.Skia.UnitTests/W3CTestSuiteTests.cs` compares rendered output against the expected PNG baselines for the supported subset.

## UI tests

Avalonia-related packages are also covered with UI-oriented tests where appropriate, including the `Avalonia.Svg.Skia.UiTests` project in the solution.

## Source-generator stress scripts

The `tests-sourcegenerators/` folder contains scripts that exercise generated-code workflows against larger input sets:

- `W3CTestSuite.sh`
- `fluentui-system-icons.sh`
- `resvg-test-suite.sh`

These scripts clone external repositories, run `svgc`, create console projects, add SkiaSharp dependencies, and validate that the generated code compiles.

## Why this matters

This repository does more than simple bitmap conversion. It spans:

- document parsing,
- model conversion,
- runtime rendering,
- Avalonia integration,
- generated code,
- Android VectorDrawable import.

The layered test strategy reflects that larger surface area.
