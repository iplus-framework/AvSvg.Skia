#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace Svg;

/// <summary>
/// Browser-compatibility focused loader for Svg.Custom.
///
/// The upstream loader is good enough for basic SVG parsing, but the Chrome-backed W3C rows showed
/// two loader-specific gaps that matter before CSS can be applied correctly:
///
/// 1. relative stylesheet references need a stable document base URI, even when the SVG is opened
///    through the convenience API and CSS is applied after parsing;
/// 2. the XML/tree-loading path needs to preserve the raw stylesheet text so the stricter
///    browser-compatibility CSS pass can run after the document model is built.
///
/// This class keeps the upstream XML tree construction shape and delegates the Chrome-aligned CSS
/// policy to <see cref="SvgCssCompatibilityProcessor"/> once the raw style sources have been
/// collected.
/// </summary>
public static class SvgDocumentCompatibilityLoader
{
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
        var styles = new List<SvgCssStyleSource>();
        var elementFactory = new SvgElementFactory();
        var svgDocument = Create<T>(reader, elementFactory, styles, baseUri);

        if (css is not null)
        {
            styles.Add(new SvgCssStyleSource(css, baseUri));
        }

        SvgCssCompatibilityProcessor.Apply(svgDocument!, styles, elementFactory);
        svgDocument?.FlushStyles(true);
        return svgDocument!;
    }

    private static T Create<T>(XmlReader reader, SvgElementFactory elementFactory, List<SvgCssStyleSource> styles, Uri? baseUri)
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
                            SvgCssCompatibilityProcessor.ShouldApplyStyleElement(unknown))
                        {
                            // Preserve the document base URI with every collected <style> block so
                            // any nested @import inside that block resolves relative to the SVG file
                            // that declared it, not to the current process working directory.
                            styles.Add(new SvgCssStyleSource(unknown.Content ?? string.Empty, svgDocument?.BaseUri));
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

}
