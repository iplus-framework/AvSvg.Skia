#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using ExCSS;
using Svg.Css;

namespace Svg;

public static class SvgDocumentCompatibilityLoader
{
    public static T Open<T>(string path, SvgOptions svgOptions) where T : SvgDocument, new()
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        using var stream = File.OpenRead(path);
        var document = Open<T>(stream, svgOptions);
        document.BaseUri = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        return document;
    }

    public static T Open<T>(Stream stream, SvgOptions svgOptions) where T : SvgDocument, new()
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

        return Create<T>(reader, svgOptions.Css);
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

        using var svgReader = XmlReader.Create(reader, new XmlReaderSettings
        {
            XmlResolver = new SvgDtdResolver(),
            DtdProcessing = DtdProcessing.Parse,
            IgnoreWhitespace = false,
        });

        return Create<T>(svgReader);
    }

    private static T Create<T>(XmlReader reader, string? css = null) where T : SvgDocument, new()
    {
        var styles = new List<ISvgNode>();
        var elementFactory = new SvgElementFactory();
        var svgDocument = Create<T>(reader, elementFactory, styles);

        if (css is not null)
        {
            styles.Add(new SvgUnknownElement { Content = css });
        }

        if (styles.Any())
        {
            var cssTotal = string.Join(Environment.NewLine, styles.Select(s => s.Content).ToArray());
            var stylesheetParser = new StylesheetParser(true, true, tolerateInvalidValues: true);
            var stylesheet = stylesheetParser.Parse(cssTotal);

            foreach (var rule in stylesheet.StyleRules)
            {
                try
                {
                    var rootNode = new NonSvgElement();
                    rootNode.Children.Add(svgDocument);

                    var elemsToStyle = rootNode.QuerySelectorAll(rule.Selector, elementFactory);
                    foreach (var elem in elemsToStyle)
                    {
                        foreach (var declaration in rule.Style)
                        {
                            elem.AddStyle(declaration.Name, declaration.Original, rule.Selector.GetSpecificity());
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
        return svgDocument;
    }

    private static T Create<T>(XmlReader reader, SvgElementFactory elementFactory, List<ISvgNode> styles)
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

                        if (element is SvgUnknownElement unknown && unknown.ElementName == "style")
                        {
                            styles.Add(unknown);
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
}
