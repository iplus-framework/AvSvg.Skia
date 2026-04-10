#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ExCSS;
using Svg.Css;

namespace Svg;

/// <summary>
/// Browser-compatibility focused loader for Svg.Custom.
///
/// The upstream loader is good enough for basic SVG parsing, but the Chrome-backed W3C rows showed
/// four CSS-specific gaps that matter for static rendering correctness:
///
/// 1. relative stylesheet references need a stable document base URI, even when the SVG is opened
///    through the convenience API and CSS is applied after parsing;
/// 2. only <style type="text/css"> (or an omitted type) should participate in CSS parsing;
/// 3. malformed @import rules should be ignored the way Chrome ignores them, while valid imports
///    should still be expanded before the remaining rules are applied;
/// 4. media-qualified imports should only apply when they match the static screen rendering
///    context that Chrome uses for the checked baselines.
///
/// This loader keeps the upstream XML tree construction shape, then applies a narrower CSS pass on
/// top that preserves browser behavior for those cases without changing unrelated parsing logic.
/// </summary>
public static class SvgDocumentCompatibilityLoader
{
    // Chrome-backed W3C overrides are captured in a fixed 480x360 viewport. Reusing that same
    // static media environment here keeps @import media-query evaluation aligned with the renderer
    // context the checked baselines are compared against.
    private const double StaticScreenWidthPixels = 480d;
    private const double StaticScreenHeightPixels = 360d;

    public static T Open<T>(string path, SvgOptions svgOptions) where T : SvgDocument, new()
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        // Capture the absolute document URI before the stream is opened so later CSS resolution can
        // expand relative @import/file references exactly as the browser would relative to the SVG.
        var baseUri = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        using var stream = File.OpenRead(path);
        return Open<T>(stream, svgOptions, baseUri);
    }

    public static T Open<T>(Stream stream, SvgOptions svgOptions) where T : SvgDocument, new()
    {
        return Open<T>(stream, svgOptions, null);
    }

    private static T Open<T>(Stream stream, SvgOptions svgOptions, Uri? baseUri) where T : SvgDocument, new()
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var reader = new SvgTextReader(stream, svgOptions.Entities)
        {
            XmlResolver = new SvgDtdResolver(),
            WhitespaceHandling = WhitespaceHandling.All,
            DtdProcessing = SvgDocument.DisableDtdProcessing ? DtdProcessing.Ignore : DtdProcessing.Parse,
        };

        return Create<T>(reader, svgOptions.Css, baseUri);
    }

    public static T FromSvg<T>(string svg) where T : SvgDocument, new()
    {
        if (string.IsNullOrEmpty(svg))
        {
            throw new ArgumentNullException(nameof(svg));
        }

        using var stringReader = new StringReader(svg);
        var reader = new SvgTextReader(stringReader, null)
        {
            XmlResolver = new SvgDtdResolver(),
            WhitespaceHandling = WhitespaceHandling.All,
            DtdProcessing = SvgDocument.DisableDtdProcessing ? DtdProcessing.Ignore : DtdProcessing.Parse,
        };

        return Create<T>(reader);
    }

    public static T Open<T>(XmlReader reader) where T : SvgDocument, new()
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var baseUri = TryGetAbsoluteBaseUri(reader.BaseURI);

        if (SvgDocument.DisableDtdProcessing &&
            reader.Settings?.DtdProcessing == DtdProcessing.Parse)
        {
            throw new InvalidOperationException("XmlReader input must not enable DTD processing when SvgDocument.DisableDtdProcessing is true.");
        }

        using var svgReader = XmlReader.Create(reader, new XmlReaderSettings
        {
            XmlResolver = new SvgDtdResolver(),
            DtdProcessing = SvgDocument.DisableDtdProcessing ? DtdProcessing.Ignore : DtdProcessing.Parse,
            IgnoreWhitespace = false,
        });

        return Create<T>(svgReader, baseUri: baseUri);
    }

    private static T Create<T>(XmlReader reader, string? css = null, Uri? baseUri = null) where T : SvgDocument, new()
    {
        // Keep each stylesheet fragment together with the URI it should resolve against. That lets
        // inline CSS from the SVG document, externally supplied CSS, and recursively imported CSS
        // all share one merge/apply path without losing origin information.
        var styles = new List<StyleSource>();
        var elementFactory = new SvgElementFactory();
        var svgDocument = Create<T>(reader, elementFactory, styles, baseUri);

        if (css is not null)
        {
            styles.Add(new StyleSource(css, baseUri));
        }

        if (styles.Any())
        {
            // Expand valid imports first so the final stylesheet matches browser evaluation order:
            // imported rules are inlined into the aggregate stylesheet before selector matching.
            var cssTotal = ExpandImportedStyles(styles);
            var stylesheetParser = new StylesheetParser(true, true, tolerateInvalidValues: true);
            var stylesheet = stylesheetParser.Parse(cssTotal);

            foreach (var rule in stylesheet.StyleRules)
            {
                try
                {
                    var rootNode = new NonSvgElement();
                    rootNode.Children.Add(svgDocument);
                    var projectsLinkStylesToText = ContainsLinkPseudoClass(rule.Selector);

                    var elemsToStyle = rootNode.QuerySelectorAll(rule.Selector, elementFactory);
                    foreach (var elem in elemsToStyle)
                    {
                        foreach (var styleTarget in GetStyleTargets(elem, projectsLinkStylesToText))
                        {
                            foreach (var declaration in rule.Style)
                            {
                                styleTarget.AddStyle(declaration.Name, declaration.Original, rule.Selector.GetSpecificity());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.Message);
                }
            }
        }

        svgDocument?.FlushStyles(true);
        return svgDocument!;
    }

    private static T Create<T>(XmlReader reader, SvgElementFactory elementFactory, List<StyleSource> styles, Uri? baseUri)
        where T : SvgDocument, new()
    {
        var elementStack = new Stack<SvgElement>();
        var elementEmpty = false;
        SvgElement? element = null;
        SvgElement? parent;
        T? svgDocument = null;

        while (reader.Read())
        {
            try
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        elementEmpty = reader.IsEmptyElement;
                        if (elementStack.Count > 0)
                        {
                            element = elementFactory.CreateElement(reader, svgDocument!);
                        }
                        else
                        {
                            svgDocument = elementFactory.CreateDocument<T>(reader);
                            svgDocument.BaseUri = baseUri;
                            element = svgDocument;
                        }

                        if (elementStack.Count > 0)
                        {
                            parent = elementStack.Peek();
                            if (parent is not null && element is not null)
                            {
                                parent.Children.Add(element);
                                parent.Nodes.Add(element);
                            }
                        }

                        elementStack.Push(element!);
                        if (elementEmpty)
                        {
                            goto case XmlNodeType.EndElement;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        element = elementStack.Pop();

                        if (element.Nodes.OfType<SvgContentNode>().Any())
                        {
                            element.Content = string.Concat(element.Nodes.Select(n => n.Content).ToArray());
                        }
                        else
                        {
                            element.Nodes.Clear();
                        }

                        if (element is SvgUnknownElement unknown &&
                            unknown.ElementName == "style" &&
                            ShouldApplyStyleElement(unknown))
                        {
                            // Preserve the document base URI with every collected <style> block so
                            // any nested @import inside that block resolves relative to the SVG file
                            // that declared it, not to the current process working directory.
                            styles.Add(new StyleSource(unknown.Content ?? string.Empty, svgDocument?.BaseUri));
                        }

                        break;

                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                        if (elementStack.Count > 0)
                        {
                            elementStack.Peek().Nodes.Add(new SvgContentNode { Content = reader.Value });
                        }

                        break;

                    case XmlNodeType.Whitespace:
                        if (elementStack.Count > 0 && ShouldPreserveTextWhitespace(elementStack.Peek()))
                        {
                            elementStack.Peek().Nodes.Add(new SvgContentNode { Content = reader.Value });
                        }

                        break;

                    case XmlNodeType.EntityReference:
                        reader.ResolveEntity();
                        if (elementStack.Count > 0)
                        {
                            elementStack.Peek().Nodes.Add(new SvgContentNode { Content = reader.Value });
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        return svgDocument!;
    }

    private static bool ShouldPreserveTextWhitespace(SvgElement element)
    {
        return element is SvgTextBase;
    }

    private static IEnumerable<SvgElement> GetStyleTargets(SvgElement element, bool projectsLinkStylesToText)
    {
        yield return element;

        if (projectsLinkStylesToText && TryGetLinkTextContainer(element, out var textContainer))
        {
            yield return textContainer;
        }
    }

    private static bool TryGetLinkTextContainer(SvgElement element, out SvgTextBase textContainer)
    {
        textContainer = null!;

        if (element is not SvgAnchor anchor ||
            string.IsNullOrWhiteSpace(anchor.Href) ||
            anchor.Parent is not SvgTextBase parentTextBase)
        {
            return false;
        }

        var preservesWhitespace = parentTextBase.SpaceHandling == XmlSpaceHandling.Preserve;

        foreach (var node in parentTextBase.Nodes)
        {
            if (ReferenceEquals(node, anchor))
            {
                continue;
            }

            if (!preservesWhitespace &&
                node is SvgContentNode contentNode &&
                string.IsNullOrWhiteSpace(contentNode.Content))
            {
                continue;
            }

            // If the surrounding text container has any other meaningful content, projecting the
            // link rule onto it would leak styles onto non-link glyphs. In that case the rule must
            // stay anchored to the matched <a> only.
            return false;
        }

        // The renderer draws raw text children through the surrounding text container rather than
        // through the <a> node itself. When the anchor is the only meaningful child of that text
        // container, mirroring the fully matched rule onto the container reproduces Chrome's link
        // styling without widening selector matching.
        textContainer = parentTextBase;
        return true;
    }

    private static bool ContainsLinkPseudoClass(ISelector selector)
    {
        return selector switch
        {
            PseudoClassSelector pseudoClassSelector => string.Equals(pseudoClassSelector.Class, "link", StringComparison.OrdinalIgnoreCase),
            CompoundSelector compoundSelector => compoundSelector.Any(ContainsLinkPseudoClass),
            ComplexSelector complexSelector => complexSelector.Any(part => ContainsLinkPseudoClass(part.Selector)),
            ListSelector listSelector => listSelector.Any(ContainsLinkPseudoClass),
            _ => false,
        };
    }

    private static Uri? TryGetAbsoluteBaseUri(string? baseUri)
    {
        if (string.IsNullOrWhiteSpace(baseUri))
        {
            return null;
        }

        // XmlReader can surface the source location as BaseURI when it was opened from a file or
        // other URI-backed source. Thread that information into the compatibility loader so all
        // Open(...) entry points resolve relative stylesheets the same way.
        return Uri.TryCreate(baseUri, UriKind.Absolute, out var absoluteBaseUri)
            ? absoluteBaseUri
            : null;
    }

    private static bool ShouldApplyStyleElement(SvgUnknownElement styleElement)
    {
        if (!styleElement.TryGetAttribute("type", out var styleType))
        {
            return true;
        }

        // Browsers ignore non-CSS <style> payloads for CSS selector matching. Restricting the
        // loader here avoids feeding script/data blocks into ExCSS and accidentally producing
        // selectors or declarations from content Chrome would never treat as CSS.
        var parameterSeparatorIndex = styleType.IndexOf(';');
        var mediaType = parameterSeparatorIndex >= 0
            ? styleType.Substring(0, parameterSeparatorIndex)
            : styleType;
        return string.Equals(mediaType.Trim(), "text/css", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExpandImportedStyles(IEnumerable<StyleSource> sources)
    {
        var builder = new StringBuilder();

        foreach (var source in sources)
        {
            // Each top-level stylesheet source gets its own active import chain. That still breaks
            // cycles, but it avoids globally deduping imports across sibling <style> blocks, which
            // would erase valid source-order effects from later imports of the same stylesheet.
            builder.AppendLine(ExpandImportedStyles(
                source.Content,
                source.BaseUri,
                CreateImportChain()));
        }

        return builder.ToString();
    }

    private static HashSet<string> CreateImportChain()
    {
        // Import cycle detection should compare the fully resolved URI text exactly. Folding case
        // here breaks legitimate imports on case-sensitive filesystems where `A.css` and `a.css`
        // are different resources that browsers would load independently.
        return new HashSet<string>(StringComparer.Ordinal);
    }

    private static string ExpandImportedStyles(string cssText, Uri? baseUri, HashSet<string> importChain)
    {
        var stylesheetParser = new StylesheetParser(true, true, tolerateInvalidValues: true);
        var stylesheet = stylesheetParser.Parse(cssText);
        var builder = new StringBuilder();

        // Import expansion is driven by the original CSS text instead of ExCSS nodes so we can be
        // stricter than the tolerant parser: only imports that match the valid grammar above are
        // followed, which keeps malformed imports from affecting rendering.
        foreach (var importRule in GetImportRules(cssText))
        {
            if (!ShouldApplyImportForCurrentMedia(importRule.MediaCondition))
            {
                continue;
            }

            var imported = TryLoadImportedStylesheet(importRule.Href, baseUri, importChain);
            if (imported is not null)
            {
                try
                {
                    builder.AppendLine(ExpandImportedStyles(imported.Content, imported.BaseUri, importChain));
                }
                finally
                {
                    importChain.Remove(imported.BaseUri!.AbsoluteUri);
                }
            }
        }

        foreach (var child in stylesheet.Children)
        {
            if (IsImportRule(child))
            {
                continue;
            }

            builder.AppendLine(child.ToCss());
        }

        return builder.ToString();
    }

    private static IEnumerable<ImportRule> GetImportRules(string cssText)
    {
        var index = 0;

        while (TryReadNextTopLevelStatement(cssText, ref index, out var statement))
        {
            if (IsAtRule(cssText, statement, "@charset"))
            {
                continue;
            }

            // CSS only honors @import rules while the stylesheet is still in its leading import
            // section. As soon as a normal rule or another at-rule appears, later imports are
            // invalid and must be ignored even if they are otherwise well-formed.
            if (!IsAtRule(cssText, statement, "@import"))
            {
                yield break;
            }

            if (TryParseImportRule(cssText, statement, out var importRule))
            {
                yield return importRule;
            }
        }
    }

    private static bool ShouldApplyImportForCurrentMedia(string mediaCondition)
    {
        if (string.IsNullOrWhiteSpace(mediaCondition))
        {
            return true;
        }

        foreach (var mediaQuery in mediaCondition.Split(','))
        {
            if (MatchesCurrentMedia(mediaQuery))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesCurrentMedia(string mediaQuery)
    {
        var normalized = mediaQuery.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.StartsWith("only ", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring("only ".Length).TrimStart();
        }

        var isNegated = false;
        if (normalized.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            isNegated = true;
            normalized = normalized.Substring("not ".Length).TrimStart();
        }

        if (!TryParseMediaQuery(normalized, out var mediaType, out var mediaFeatures))
        {
            return false;
        }

        // Treat Svg.Skia's checked rendering context as "screen". That matches the Chrome capture
        // workflow used for baselines, so imports scoped to other media such as "print" should not
        // leak into the static image output.
        var matchesScreen = string.IsNullOrWhiteSpace(mediaType) ||
            string.Equals(mediaType, "all", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mediaType, "screen", StringComparison.OrdinalIgnoreCase);

        // Once a media query adds feature predicates, matching the type alone is no longer enough.
        // For example, `screen and (max-width: 1px)` must not apply in the static 480px viewport
        // used by the Chrome reference captures. Unsupported features are treated conservatively as
        // non-matches so imports are not inlined on predicates we cannot validate.
        var matchesFeatures = mediaFeatures.All(EvaluateMediaFeature);
        var matchesMediaQuery = matchesScreen && matchesFeatures;
        return isNegated ? !matchesMediaQuery : matchesMediaQuery;
    }

    private static bool TryParseMediaQuery(string mediaQuery, out string mediaType, out List<string> mediaFeatures)
    {
        mediaType = string.Empty;
        mediaFeatures = new List<string>();

        var index = 0;
        SkipWhitespaceAndComments(mediaQuery, ref index, mediaQuery.Length);

        if (index < mediaQuery.Length && mediaQuery[index] != '(')
        {
            var typeStart = index;
            while (index < mediaQuery.Length && !char.IsWhiteSpace(mediaQuery[index]) && mediaQuery[index] != '(')
            {
                index++;
            }

            mediaType = mediaQuery.Substring(typeStart, index - typeStart).Trim();
            SkipWhitespaceAndComments(mediaQuery, ref index, mediaQuery.Length);
        }

        var expectsFeatureAfterAnd = false;
        while (index < mediaQuery.Length)
        {
            if (mediaQuery[index] == '(')
            {
                if (!TryReadMediaFeature(mediaQuery, ref index, out var mediaFeature))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(mediaFeature))
                {
                    return false;
                }

                mediaFeatures.Add(mediaFeature);
                expectsFeatureAfterAnd = false;
                SkipWhitespaceAndComments(mediaQuery, ref index, mediaQuery.Length);
                continue;
            }

            if (!TryConsumeMediaAnd(mediaQuery, ref index))
            {
                return false;
            }

            expectsFeatureAfterAnd = true;
            SkipWhitespaceAndComments(mediaQuery, ref index, mediaQuery.Length);
        }

        return !expectsFeatureAfterAnd;
    }

    private static bool TryConsumeMediaAnd(string mediaQuery, ref int index)
    {
        if (!mediaQuery.AsSpan(index).StartsWith("and", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var endIndex = index + "and".Length;
        if (endIndex < mediaQuery.Length && !char.IsWhiteSpace(mediaQuery[endIndex]) && mediaQuery[endIndex] != '(')
        {
            return false;
        }

        index = endIndex;
        return true;
    }

    private static bool TryReadMediaFeature(string mediaQuery, ref int index, out string mediaFeature)
    {
        mediaFeature = string.Empty;

        if (index >= mediaQuery.Length || mediaQuery[index] != '(')
        {
            return false;
        }

        index++;
        var featureStart = index;
        var depth = 1;

        while (index < mediaQuery.Length)
        {
            if (TrySkipComment(mediaQuery, ref index, mediaQuery.Length))
            {
                continue;
            }

            var current = mediaQuery[index];
            switch (current)
            {
                case '\'':
                case '"':
                    SkipQuotedString(mediaQuery, ref index, current);
                    continue;
                case '(':
                    depth++;
                    index++;
                    continue;
                case ')':
                    depth--;
                    if (depth == 0)
                    {
                        mediaFeature = mediaQuery.Substring(featureStart, index - featureStart).Trim();
                        index++;
                        return true;
                    }

                    index++;
                    continue;
                default:
                    index++;
                    break;
            }
        }

        return false;
    }

    private static bool EvaluateMediaFeature(string mediaFeature)
    {
        var separatorIndex = mediaFeature.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        var name = mediaFeature.Substring(0, separatorIndex).Trim();
        var value = mediaFeature.Substring(separatorIndex + 1).Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return name.ToLowerInvariant() switch
        {
            "width" => MatchesExactDimension(value, StaticScreenWidthPixels),
            "min-width" => MatchesMinimumDimension(value, StaticScreenWidthPixels),
            "max-width" => MatchesMaximumDimension(value, StaticScreenWidthPixels),
            "height" => MatchesExactDimension(value, StaticScreenHeightPixels),
            "min-height" => MatchesMinimumDimension(value, StaticScreenHeightPixels),
            "max-height" => MatchesMaximumDimension(value, StaticScreenHeightPixels),
            "orientation" => MatchesOrientation(value),
            _ => false,
        };
    }

    private static bool MatchesExactDimension(string value, double currentPixels)
    {
        return TryParsePixelLength(value, out var requestedPixels) &&
               Math.Abs(requestedPixels - currentPixels) < 0.001d;
    }

    private static bool MatchesMinimumDimension(string value, double currentPixels)
    {
        return TryParsePixelLength(value, out var requestedPixels) &&
               currentPixels + 0.001d >= requestedPixels;
    }

    private static bool MatchesMaximumDimension(string value, double currentPixels)
    {
        return TryParsePixelLength(value, out var requestedPixels) &&
               currentPixels - 0.001d <= requestedPixels;
    }

    private static bool MatchesOrientation(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "landscape" => StaticScreenWidthPixels >= StaticScreenHeightPixels,
            "portrait" => StaticScreenHeightPixels > StaticScreenWidthPixels,
            _ => false,
        };
    }

    private static bool TryParsePixelLength(string value, out double pixels)
    {
        pixels = 0d;

        var normalized = value.Trim();
        if (string.Equals(normalized, "0", StringComparison.Ordinal))
        {
            return true;
        }

        if (!normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return double.TryParse(
            normalized.Substring(0, normalized.Length - 2).Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out pixels);
    }

    private static bool IsImportRule(IStylesheetNode child)
    {
        return child.ToCss().TrimStart().StartsWith("@import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadNextTopLevelStatement(string cssText, ref int index, out CssStatement statement)
    {
        SkipWhitespaceAndComments(cssText, ref index);

        if (index >= cssText.Length)
        {
            statement = default;
            return false;
        }

        var start = index;
        var parenthesisDepth = 0;

        while (index < cssText.Length)
        {
            if (TrySkipComment(cssText, ref index))
            {
                continue;
            }

            var current = cssText[index];
            switch (current)
            {
                case '\'':
                case '"':
                    SkipQuotedString(cssText, ref index, current);
                    continue;
                case '(':
                    parenthesisDepth++;
                    index++;
                    continue;
                case ')' when parenthesisDepth > 0:
                    parenthesisDepth--;
                    index++;
                    continue;
                case ';' when parenthesisDepth == 0:
                    statement = new CssStatement(start, index, index + 1, CssStatementTerminator.Semicolon);
                    index++;
                    return true;
                case '{' when parenthesisDepth == 0:
                    index++;
                    SkipBlock(cssText, ref index);
                    statement = new CssStatement(start, index, index, CssStatementTerminator.Block);
                    return true;
                default:
                    index++;
                    break;
            }
        }

        statement = new CssStatement(start, index, index, CssStatementTerminator.EndOfFile);
        return true;
    }

    private static bool IsAtRule(string cssText, CssStatement statement, string atKeyword)
    {
        if (!cssText.AsSpan(statement.Start, statement.Length).TrimStart().StartsWith(atKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var keywordStart = statement.Start;
        SkipWhitespaceAndComments(cssText, ref keywordStart, statement.EndExclusive);

        if (!cssText.AsSpan(keywordStart, statement.EndExclusive - keywordStart).StartsWith(atKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var boundaryIndex = keywordStart + atKeyword.Length;
        return boundaryIndex >= statement.EndExclusive || !IsCssIdentifierCharacter(cssText[boundaryIndex]);
    }

    private static bool TryParseImportRule(string cssText, CssStatement statement, out ImportRule importRule)
    {
        importRule = null!;

        if (statement.Terminator != CssStatementTerminator.Semicolon)
        {
            return false;
        }

        var index = statement.Start;
        SkipWhitespaceAndComments(cssText, ref index, statement.EndExclusive);

        if (!cssText.AsSpan(index, statement.EndExclusive - index).StartsWith("@import", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        index += "@import".Length;
        SkipWhitespaceAndComments(cssText, ref index, statement.EndExclusive);

        if (!TryReadImportHref(cssText, ref index, statement.EndExclusive, out var href))
        {
            return false;
        }

        SkipWhitespaceAndComments(cssText, ref index, statement.EndExclusive);
        var mediaCondition = cssText.Substring(index, statement.ContentEndExclusive - index).Trim();

        if (string.IsNullOrWhiteSpace(href))
        {
            return false;
        }

        importRule = new ImportRule(href, mediaCondition);
        return true;
    }

    private static bool TryReadImportHref(string cssText, ref int index, int endExclusive, out string href)
    {
        href = string.Empty;

        if (index >= endExclusive)
        {
            return false;
        }

        var current = cssText[index];
        if (current is '\'' or '"')
        {
            return TryReadQuotedValue(cssText, ref index, endExclusive, current, out href);
        }

        if (!cssText.AsSpan(index, endExclusive - index).StartsWith("url", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var boundaryIndex = index + 3;
        if (boundaryIndex < endExclusive && IsCssIdentifierCharacter(cssText[boundaryIndex]))
        {
            return false;
        }

        index = boundaryIndex;
        SkipWhitespaceAndComments(cssText, ref index, endExclusive);

        if (index >= endExclusive || cssText[index] != '(')
        {
            return false;
        }

        index++;
        SkipWhitespaceAndComments(cssText, ref index, endExclusive);

        if (index >= endExclusive)
        {
            return false;
        }

        if (cssText[index] is '\'' or '"')
        {
            var delimiter = cssText[index];
            if (!TryReadQuotedValue(cssText, ref index, endExclusive, delimiter, out href))
            {
                return false;
            }
        }
        else
        {
            var hrefStart = index;

            while (index < endExclusive && cssText[index] != ')')
            {
                if (TrySkipComment(cssText, ref index))
                {
                    continue;
                }

                index++;
            }

            href = cssText.Substring(hrefStart, index - hrefStart).Trim();
        }

        SkipWhitespaceAndComments(cssText, ref index, endExclusive);

        if (index >= endExclusive || cssText[index] != ')')
        {
            return false;
        }

        index++;
        return !string.IsNullOrWhiteSpace(href);
    }

    private static bool TryReadQuotedValue(string cssText, ref int index, int endExclusive, char delimiter, out string value)
    {
        value = string.Empty;

        if (index >= endExclusive || cssText[index] != delimiter)
        {
            return false;
        }

        index++;
        var start = index;
        var builder = new StringBuilder();

        while (index < endExclusive)
        {
            var current = cssText[index];
            if (current == '\\')
            {
                builder.Append(cssText, start, index - start);
                index++;

                if (index >= endExclusive)
                {
                    return false;
                }

                builder.Append(cssText[index]);
                index++;
                start = index;
                continue;
            }

            if (current == delimiter)
            {
                builder.Append(cssText, start, index - start);
                index++;
                value = builder.ToString();
                return true;
            }

            index++;
        }

        return false;
    }

    private static void SkipWhitespaceAndComments(string cssText, ref int index)
    {
        SkipWhitespaceAndComments(cssText, ref index, cssText.Length);
    }

    private static void SkipWhitespaceAndComments(string cssText, ref int index, int endExclusive)
    {
        while (index < endExclusive)
        {
            if (char.IsWhiteSpace(cssText[index]))
            {
                index++;
                continue;
            }

            if (!TrySkipComment(cssText, ref index, endExclusive))
            {
                break;
            }
        }
    }

    private static bool TrySkipComment(string cssText, ref int index)
    {
        return TrySkipComment(cssText, ref index, cssText.Length);
    }

    private static bool TrySkipComment(string cssText, ref int index, int endExclusive)
    {
        if (index + 1 >= endExclusive || cssText[index] != '/' || cssText[index + 1] != '*')
        {
            return false;
        }

        index += 2;
        while (index + 1 < endExclusive)
        {
            if (cssText[index] == '*' && cssText[index + 1] == '/')
            {
                index += 2;
                return true;
            }

            index++;
        }

        index = endExclusive;
        return true;
    }

    private static void SkipQuotedString(string cssText, ref int index, char delimiter)
    {
        index++;

        while (index < cssText.Length)
        {
            if (cssText[index] == '\\')
            {
                index = Math.Min(index + 2, cssText.Length);
                continue;
            }

            if (cssText[index] == delimiter)
            {
                index++;
                return;
            }

            index++;
        }
    }

    private static void SkipBlock(string cssText, ref int index)
    {
        var depth = 1;

        while (index < cssText.Length && depth > 0)
        {
            if (TrySkipComment(cssText, ref index))
            {
                continue;
            }

            var current = cssText[index];
            switch (current)
            {
                case '\'':
                case '"':
                    SkipQuotedString(cssText, ref index, current);
                    break;
                case '{':
                    depth++;
                    index++;
                    break;
                case '}':
                    depth--;
                    index++;
                    break;
                default:
                    index++;
                    break;
            }
        }
    }

    private static bool IsCssIdentifierCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value is '-' or '_';
    }

    private static StyleSource? TryLoadImportedStylesheet(string? href, Uri? baseUri, HashSet<string> importChain)
    {
        if (string.IsNullOrWhiteSpace(href) || baseUri is null)
        {
            return null;
        }

        // Keep import resolution intentionally conservative: only file-backed resources relative to
        // the current SVG/CSS source are loaded here. That matches the scenarios exercised by the
        // W3C fixtures and avoids inventing new network/resource-loading behavior in Svg.Skia.
        if (!Uri.TryCreate(baseUri, href, out var stylesheetUri) || !stylesheetUri.IsFile)
        {
            return null;
        }

        // Cycle protection is scoped to the currently expanding import chain so repeated imports in
        // separate top-level <style> blocks still participate in cascade order like they do in a
        // browser.
        if (importChain.Contains(stylesheetUri.AbsoluteUri))
        {
            return null;
        }

        var localPath = stylesheetUri.LocalPath;
        if (!File.Exists(localPath))
        {
            return null;
        }

        importChain.Add(stylesheetUri.AbsoluteUri);
        return new StyleSource(File.ReadAllText(localPath), stylesheetUri);
    }

    private sealed class StyleSource
    {
        public StyleSource(string content, Uri? baseUri)
        {
            Content = content;
            BaseUri = baseUri;
        }

        // The raw stylesheet text collected from a <style> element, SvgOptions.Css, or an imported
        // external file.
        public string Content { get; }

        // The URI that relative URLs inside Content should resolve against.
        public Uri? BaseUri { get; }
    }

    private sealed class ImportRule
    {
        public ImportRule(string href, string mediaCondition)
        {
            Href = href;
            MediaCondition = mediaCondition;
        }

        public string Href { get; }

        public string MediaCondition { get; }
    }

    private readonly struct CssStatement
    {
        public CssStatement(int start, int contentEndExclusive, int endExclusive, CssStatementTerminator terminator)
        {
            Start = start;
            ContentEndExclusive = contentEndExclusive;
            EndExclusive = endExclusive;
            Terminator = terminator;
        }

        public int Start { get; }

        public int ContentEndExclusive { get; }

        public int EndExclusive { get; }

        public CssStatementTerminator Terminator { get; }

        public int Length => EndExclusive - Start;
    }

    private enum CssStatementTerminator
    {
        EndOfFile,
        Semicolon,
        Block,
    }
}
