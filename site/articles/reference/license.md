---
title: "License"
---

# License

## Repository license

The repository root license file is MIT:

- `LICENSE.TXT`

## Vendored SVG package

`Svg.Custom` packages vendored source from `externals/SVG`, and that upstream source carries the Microsoft Public License (Ms-PL):

- `externals/SVG/license.txt`
- `src/Svg.Custom/Svg.Custom.csproj` declares `PackageLicenseExpression` as `MS-PL`

## Practical implication

When consuming or redistributing packages from this repository, check the package-level license metadata in addition to the repository root license. The repo is not a single-license story once the vendored SVG source is included.
