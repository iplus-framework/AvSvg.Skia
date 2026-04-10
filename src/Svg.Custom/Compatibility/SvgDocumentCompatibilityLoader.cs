#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    // ExCSS parses forgivingly, which is useful for most stylesheet recovery, but it also means an
    // invalid @import can survive long enough to be interpreted differently from Chrome. We first
    // identify only syntactically valid import forms and expand those; everything else is left in
    // place for the normal parser and, if invalid, naturally ignored.
    private static readonly Regex ImportRuleRegex = new(
        """
        @import\s+
        (?:
            url\(\s*(?<url>[^)]+?)\s*\)
            |
            (?<quoted>"[^"]+"|'[^']+')
        )
        (?<media>[^;]*);
        """,
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

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
                            styles.Add(new StyleSource(unknown.Content, svgDocument?.BaseUri));
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

        foreach (var node in parentTextBase.Nodes)
        {
            if (ReferenceEquals(node, anchor))
            {
                continue;
            }

            if (node is SvgContentNode contentNode &&
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
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
        }

        return builder.ToString();
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
        foreach (Match match in ImportRuleRegex.Matches(cssText))
        {
            // The regex already filtered for supported, syntactically valid @import forms; here we
            // only normalize the captured URL token before passing it into URI resolution.
            var rawHref = match.Groups["url"].Success
                ? match.Groups["url"].Value
                : match.Groups["quoted"].Value;
            var href = rawHref.Trim().Trim('"', '\'');
            if (!string.IsNullOrWhiteSpace(href))
            {
                yield return new ImportRule(href, match.Groups["media"].Value.Trim());
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
            return true;
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

        var separatorIndex = normalized.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '(' });
        var mediaType = separatorIndex >= 0
            ? normalized.Substring(0, separatorIndex)
            : normalized;

        // Treat Svg.Skia's checked rendering context as "screen". That matches the Chrome capture
        // workflow used for baselines, so imports scoped to other media such as "print" should not
        // leak into the static image output.
        var matchesScreen = string.IsNullOrWhiteSpace(mediaType) ||
            string.Equals(mediaType, "all", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mediaType, "screen", StringComparison.OrdinalIgnoreCase);

        return isNegated ? !matchesScreen : matchesScreen;
    }

    private static bool IsImportRule(IStylesheetNode child)
    {
        return child.ToCss().TrimStart().StartsWith("@import", StringComparison.OrdinalIgnoreCase);
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
}
