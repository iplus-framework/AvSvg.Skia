# Skipped Tests Implementation Roadmap

## Scope

This document tracks the remaining skipped test surface in `Svg.Skia` and the implementation order required to remove those skips with real renderer/runtime support instead of threshold inflation or baseline workarounds.

Current skipped test concentration:

- `tests/Svg.Skia.UnitTests/W3CTestSuiteTests.cs`
  - 217 skipped rows
  - largest groups: `animate` 78, `struct` 30, `interact` 24, `text` 24, `styling` 18, `types` 15
- `tests/Svg.Skia.UnitTests/resvgTests.cs`
  - 481 skipped rows
  - largest groups: `a-font` 50, `a-text` 37, `a-writing` 23, `a-baseline` 22, `a-filter` 42
  - largest element groups: `e-text` 40, `e-textPath` 40, `e-tspan` 30, `e-image` 31, `e-marker` 20, `e-pattern` 12

## Principles

- Prefer renderer and parser fixes over test-specific workarounds.
- Keep Chrome captures as the source of truth for W3C rows that already use Chrome overrides.
- Do not fake browser-only behavior. Rows that require DOM mutation, JavaScript execution, selection APIs, or event dispatch should only be enabled after the corresponding runtime exists.
- If upstream SVG submodule changes would be required, implement the compatibility layer in `src/Svg.Custom` instead.

## Current State

Completed in the current text tranche:

- whitespace preservation for text parents via `src/Svg.Custom/Compatibility/SvgDocumentCompatibilityLoader.cs`
- `tref` opt-in control through `ISvgTextReferenceRenderingOptions`
- Chrome-backed enablement for the first W3C text block (`text-align-*`, `text-fonts-*`, `text-path-*`, `text-tref-*`, `text-tspan-*`, `text-ws-*`, selected `text-intro-*`, selected `text-text-*`)
- underline and line-through drawing
- positioned glyph rotation handling
- `textPath` arc sampling and `%` `startOffset` correction
- `tref` content extraction now uses referenced text data with the `tref` element's own style and positioning instead of replaying the referenced subtree as-is
- resvg rows enabled from probe + xUnit verification:
  - `e-text-001/002/003/004/005/009/015/018/019/020/021/022/023/025/026/031/039/040/041/042`
  - `e-textPath-003/006/012/017/018/023/042/043/044`
  - `e-tref-001/002/003/006/007/008/009/010/011`
  - `e-tspan-001/002/007/008/009/010/011/012/014/015/016/018/020/021/022/026/031`
  - `a-letter-spacing-002/003/006/007`
  - `a-word-spacing-005`
- root text positioning fixes landed in `SvgSceneTextCompiler`:
  - the sequential text fast path now honors a root text element's initial `dx`/`dy`
  - relative-only multi-value `dx`/`dy` runs now use the positioned-glyph path instead of falling back to contiguous text
  - retained-scene regressions now cover both the root `dx`/`dy` origin case and multi-value relative glyph origins
- Chrome validation confirmed two resvg `tref` reference mismatches that must remain skipped unless the suite changes baseline policy:
  - `e-tref-004`: Chrome omits the external-document `tref` content while the resvg PNG expects it
  - `e-tref-005`: Chrome omits nested `tref` chaining content while the resvg PNG expects it
- `textPath` current-position handling is now split into two renderer rules:
  - inherited current `x` seeds path distance for the initial chunk
  - a parent `dy` list entry is consumed only by the initial `textPath` chunk instead of being reapplied to every sibling `textPath`
- This was enough to enable `e-textPath-012` and `e-textPath-023` against checked Chrome captures, but not enough to close `e-textPath-035`
- Fresh Chrome captures on 2026-04-10 confirmed another safe enablement batch:
  - `e-text-031`, `e-text-042`, and `e-textPath-003` are now browser-aligned without special thresholds
  - `a-letter-spacing-002/003/006/007` and `a-word-spacing-005` are browser-aligned but still need narrow Chrome-backed thresholds for raster-level differences
  - `e-tref-009/010` and `e-textPath-025` are now enabled after renderer fixes:
    inline `tref` content is omitted when mixed with surrounding sibling text, and a missing `textPath` geometry now aborts the remaining sibling text in that container like Chrome
  - `e-textPath-040` is now enabled after removing blanket filter suppression from retained text local-model compilation and adding nested `textPath` filter application for child runs
  - `e-tspan-026` is now enabled after two bidi/shaping fixes:
    mixed-span shaped runs now preserve visual-order segment boundaries instead of collapsing back to one segment per logical span, and trailing neutrals at the end of an LTR paragraph now resolve to the paragraph base direction instead of being forced into the preceding RTL run

Still open inside the text tranche:

- resvg `a-letter-spacing-*`
- resvg `a-word-spacing-*`
- resvg `a-textLength-*`
- resvg `a-lengthAdjust-*`
- vertical writing and baseline groups
- mixed-direction `unicode-bidi`
- browser-parity Arabic and mixed-script font fallback
- per-glyph `x/y/dx/dy` list parity in resvg `e-text-*`
- pure relative `dx/dy` multi-value positioning still differs from resvg/Chrome in `e-text-006/007/008`
- `tspan` shaping/layout across span boundaries and rotate inheritance
- advanced `textPath` layout: `text-anchor`, vertical flow, per-child positioning, underline/rotate/baseline-shift, transformed referenced paths
- consecutive `textPath` current-position parity still differs from Chrome in `e-textPath-035`
- ancestor `textLength` composition across absolutely positioned `tspan`s still differs from Chrome in `a-textLength-008`
- Chrome probes on 2026-04-10 confirmed three remaining `letter-spacing` skips are still real implementation gaps, not threshold candidates:
  - `a-letter-spacing-005`: percentage letter-spacing still differs from Chrome
  - `a-letter-spacing-008`: nested `tspan` letter-spacing distribution still differs from Chrome
  - `a-letter-spacing-009`: mixed-script Arabic letter-spacing and bidi parity still differs from Chrome

Verified probe findings from the remaining skipped W3C text rows on 2026-04-09:

- `text-align-05-b`, `text-align-06-b`, and `text-intro-03-b` do not enter any vertical placement branch in the browser-compatible text path today. The actual render stays horizontal, so these rows need a real vertical advance model, not threshold tuning.
- `text-align-08-b` is close in glyph selection but still lacks mixed-script dominant-baseline table handling across Latin, ideographic, and Devanagari glyphs.
- `text-intro-02-b` and `text-intro-09-b` still fail because mixed-direction Hebrew/Latin rows need browser-parity bidi reordering across fallback font spans. Wrapping each fallback span with bidi controls is insufficient.
- `text-intro-05-t` and `text-intro-10-f` still fail because Arabic shaping only stays correct when fallback spans are preserved, but preserving spans still leaves non-Chrome anchor/position parity. A probe to force single-run shaping produced tofu glyphs, which confirms the missing piece is mixed-font shaping/fallback support rather than a simple bidi wrapper.
- `text-altglyph-01/02/03-b` remain a separate feature area. They should not be conflated with text layout fixes because they require parsing and rendering `altGlyph`, `glyphRef`, and alternate glyph selection resources.

## Workstreams

### 1. Text Layout And Font Fidelity

Target projects:

- `src/Svg.SceneGraph`
- `src/Svg.Model`
- `src/Svg.Skia`
- `src/Svg.Custom`

Features:

- `letter-spacing`
- `word-spacing`
- `textLength`
- `lengthAdjust=spacing|spacingAndGlyphs`
- nested `tspan` rotate inheritance
- vertical `writing-mode`
- `glyph-orientation-vertical`
- mixed-direction `unicode-bidi`
- dominant/alignment baseline handling
- `altGlyph`
- webfont and SVG font fallback parity, including mixed-script and Arabic runs

Primary test impact:

- W3C `text-*`
- resvg `a-font-*`, `a-text-*`, `a-writing-*`, `a-baseline-*`, `a-textLength-*`, `e-text-*`, `e-textPath-*`, `e-tspan-*`, `e-tref-*`

Execution order:

1. Spacing, `textLength`, nested rotate inheritance, stale decoration skips
2. Vertical text flow and baseline tables
3. `unicode-bidi` mixed-direction parity
4. Webfont fallback parity
5. `altGlyph`

Acceptance criteria:

- Remaining text skips have explicit unsupported-runtime reasons or are enabled and green.
- W3C Chrome-backed rows use refreshed Chrome captures where needed.
- Resvg text groups are reduced significantly without introducing threshold-only passes.

### 2. SMIL Snapshot Rendering

Target projects:

- `src/Svg.Animation`
- `src/Svg.Skia`
- `tests/Svg.Skia.UnitTests`
- `scripts/capture_w3c_chrome_overrides.mjs`

Features:

- per-fixture animation snapshot times in W3C tests
- matching Chrome capture timing
- correct snapshot rendering for animate/set/animateTransform/animateMotion/animateColor/filter animation cases

Primary test impact:

- W3C `animate-*`
- `filters-composite-05-f`

Acceptance criteria:

- W3C animation rows no longer default to time zero when the Chrome baseline captures an advanced frame.

### 3. CSS And Styling Fidelity

Target projects:

- `src/Svg.Custom`
- `src/Svg.SceneGraph`
- `src/Svg.Model`

Features:

- class selector behavior
- inline style vs presentation attribute precedence
- inheritance edge cases
- `use` instance-tree styling semantics where feasible without full DOM runtime

Primary test impact:

- W3C `styling-*`
- W3C `struct-use-10-f`, `struct-use-11-f`
- resvg attribute/style buckets

Acceptance criteria:

- Styling rows that only depend on static cascade semantics are enabled.

### 4. Paint Servers, Filters, Images, Markers, Patterns

Target projects:

- `src/Svg.SceneGraph`
- `src/Svg.Model`
- `src/Svg.Skia`
- `src/Svg.Custom`

Features:

- image loading and fallback behavior
- marker orientation and sizing parity
- pattern inheritance and units parity
- linear/radial gradient edge cases
- remaining filter primitive fidelity

Primary test impact:

- resvg `e-image-*`, `e-marker-*`, `e-pattern-*`, `e-linearGradient-*`, `e-radialGradient-*`, `e-mask-*`, `e-filter-*`, `e-fe*`

Acceptance criteria:

- Resvg non-text skip count drops materially after renderer fixes, not by baseline swapping.

### 5. SVG DOM / Script / Interaction Runtime

Target projects:

- new runtime surface, likely centered around `src/Svg.Custom`, `src/Svg.Model`, and test harness integration

Features:

- DOM objects and live lists
- script execution
- event dispatch
- selection APIs
- mutation-driven rerendering
- interactive pointer and zoom behavior

Primary test impact:

- W3C `coords-dom-*`, `text-dom-*`, `types-dom-*`, `struct-dom-*`, `struct-svg-*`, `script-*`, `interact-*`, `text-tselect-*`

Acceptance criteria:

- These rows remain skipped until the runtime exists.
- Once started, this should be tracked as a dedicated milestone because it is not a text-rendering-only task.

## Immediate Implementation Order

The next implementation tranche should be:

1. Add a vertical text placement branch in `src/Svg.SceneGraph/SvgSceneTextCompiler.cs` for browser-compatible fallback text.
   Scope: vertical advance on Y, `text-anchor` along the vertical axis, perpendicular `baseline-shift`, and glyph rotation rules for Latin versus upright CJK.
   Acceptance: `text-align-05-b`, `text-align-06-b`, and `text-intro-03-b` render vertically against the existing Chrome captures.
2. Introduce mixed-font bidi shaping support instead of per-span bidi wrapping.
   Scope: preserve glyph fallback while shaping/reordering a single logical run, likely by adding run shaping support in the asset-loader/text-renderer layer rather than in `SvgSceneTextCompiler` alone.
   Acceptance: `text-intro-02-b` and `text-intro-09-b` match Chrome ordering, and Arabic rows no longer depend on span-local fallback behavior.
3. Finish per-glyph coordinate list parity for `e-text-006..010`, `e-text-024`, `e-tspan-013`, and the remaining positioned `tref`/`tspan` cases.
4. Finish nested `tspan` rotate inheritance and shaping across span boundaries (`e-tspan-016/017/023/024/042`).
5. Stabilize `letter-spacing` and `word-spacing` against resvg references.
6. Implement `textLength` and `lengthAdjust` using run-level metrics that match final rendered glyph advances.
7. Extend `textPath` layout for `text-anchor`, vertical flow, per-child positioning, underline/rotate/baseline-shift, and transformed referenced paths.
8. Rebaseline any newly Chrome-backed W3C rows with `node scripts/capture_w3c_chrome_overrides.mjs` after renderer changes are proven against the live Chrome capture.

## Runtime-Gated Groups

The following groups should not be enabled by changing thresholds or inventing baselines:

- W3C `text-dom-*`
- W3C `text-tselect-*`
- W3C `interact-*`
- W3C `script-*`
- W3C `types-dom-*`
- W3C `struct-dom-*`
- W3C `struct-svg-*`

They require a DOM, script, or interaction runtime rather than renderer-only fixes.

## Reference-Suite Constraints

- resvg documents `textLength` and `lengthAdjust` as unsupported in `externals/resvg/docs/unsupported.md`.
- Those rows should stay explicitly skipped in `tests/Svg.Skia.UnitTests/resvgTests.cs` unless the repository switches them to a Chrome-backed or custom Svg.Skia-native reference.
- Remaining resvg `letter-spacing` and `%` `word-spacing` failures are not parser gaps anymore; they need cluster-shaped fallback text spacing parity rather than scalar per-codepoint offsets.

## Out Of Scope For A Single Renderer Patch

These are not paint-only fixes and should not be misclassified:

- browser selection behavior
- JavaScript execution
- live DOM mutation APIs
- pointer event dispatch
- interactive zoom runtime
- DOM type inspection APIs
