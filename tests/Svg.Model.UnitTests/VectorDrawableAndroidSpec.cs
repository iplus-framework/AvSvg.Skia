using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Svg.Model.UnitTests;

internal sealed record AndroidVectorDrawableSpec(
    string PinnedTag,
    string SourceUrl,
    IReadOnlyDictionary<string, AndroidVectorDrawableElementSpec> Elements);

internal sealed record AndroidVectorDrawableElementSpec(
    string ElementName,
    IReadOnlyDictionary<string, AndroidVectorDrawableAttributeSpec> Attributes,
    IReadOnlySet<string> AllowedChildren,
    IReadOnlySet<string> RequiredAttributes);

internal sealed record AndroidVectorDrawableAttributeSpec(
    string Name,
    IReadOnlySet<string> Formats,
    IReadOnlySet<string> EnumValues,
    bool IsHidden);

internal sealed class AndroidVectorDrawableValidationResult
{
    public AndroidVectorDrawableValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public override string ToString()
    {
        return Errors.Count == 0
            ? "No validation errors."
            : string.Join(Environment.NewLine, Errors);
    }
}

internal static class AndroidVectorDrawableSpecGenerator
{
    internal const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    internal const string PinnedTag = "android-15.0.0_r1";
    internal const string PinnedSourceUrl =
        "https://android.googlesource.com/platform/frameworks/base/+/android-15.0.0_r1/core/res/res/values/attrs.xml";

    private static readonly Lazy<AndroidVectorDrawableSpec> s_pinnedSpec = new(LoadPinnedCore);

    private static readonly IReadOnlyDictionary<string, string> s_styleableToElement =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["VectorDrawable"] = "vector",
            ["VectorDrawableGroup"] = "group",
            ["VectorDrawablePath"] = "path",
            ["VectorDrawableClipPath"] = "clip-path"
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> s_allowedChildren =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["vector"] = CreateSet("group", "path", "clip-path"),
            ["group"] = CreateSet("group", "path", "clip-path"),
            ["path"] = CreateSet(),
            ["clip-path"] = CreateSet()
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> s_requiredAttributes =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["vector"] = CreateSet("width", "height", "viewportWidth", "viewportHeight"),
            ["group"] = CreateSet(),
            ["path"] = CreateSet("pathData"),
            ["clip-path"] = CreateSet("pathData")
        };

    public static AndroidVectorDrawableSpec LoadPinned() => s_pinnedSpec.Value;

    private static AndroidVectorDrawableSpec LoadPinnedCore()
    {
        var document = XDocument.Load(GetPinnedAttrsPath(), LoadOptions.PreserveWhitespace);
        var resources = document.Root ?? throw new InvalidOperationException("Pinned attrs.xml is missing a root element.");
        if (resources.Name.LocalName != "resources")
        {
            throw new InvalidOperationException("Pinned attrs.xml root element must be <resources>.");
        }

        var globalAttributes = ParseGlobalAttributeDefinitions(resources);
        var elementSpecs = new Dictionary<string, AndroidVectorDrawableElementSpec>(StringComparer.Ordinal);

        foreach (var mapping in s_styleableToElement)
        {
            var styleable = resources.Elements("declare-styleable")
                .SingleOrDefault(x => string.Equals((string?)x.Attribute("name"), mapping.Key, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Pinned attrs.xml is missing declare-styleable '{mapping.Key}'.");

            var attributes = ParseStyleableAttributes(styleable, globalAttributes);
            elementSpecs[mapping.Value] = new AndroidVectorDrawableElementSpec(
                mapping.Value,
                attributes,
                s_allowedChildren[mapping.Value],
                s_requiredAttributes[mapping.Value]);
        }

        return new AndroidVectorDrawableSpec(PinnedTag, PinnedSourceUrl, elementSpecs);
    }

    private static Dictionary<string, AndroidVectorDrawableAttributeSpec> ParseGlobalAttributeDefinitions(XElement resources)
    {
        var definitions = new Dictionary<string, AndroidVectorDrawableAttributeSpec>(StringComparer.Ordinal);
        var pendingComment = string.Empty;

        foreach (var node in resources.Nodes())
        {
            switch (node)
            {
                case XComment comment:
                    pendingComment = AppendComment(pendingComment, comment.Value);
                    break;
                case XText text when string.IsNullOrWhiteSpace(text.Value):
                    break;
                case XElement element when element.Name.LocalName == "attr":
                    var attribute = ParseAttributeDefinition(element, pendingComment, definitions);
                    definitions[attribute.Name] = attribute;
                    pendingComment = string.Empty;
                    break;
                default:
                    pendingComment = string.Empty;
                    break;
            }
        }

        return definitions;
    }

    private static Dictionary<string, AndroidVectorDrawableAttributeSpec> ParseStyleableAttributes(
        XElement styleable,
        IReadOnlyDictionary<string, AndroidVectorDrawableAttributeSpec> globalAttributes)
    {
        var attributes = new Dictionary<string, AndroidVectorDrawableAttributeSpec>(StringComparer.Ordinal);
        var pendingComment = string.Empty;

        foreach (var node in styleable.Nodes())
        {
            switch (node)
            {
                case XComment comment:
                    pendingComment = AppendComment(pendingComment, comment.Value);
                    break;
                case XText text when string.IsNullOrWhiteSpace(text.Value):
                    break;
                case XElement element when element.Name.LocalName == "attr":
                    var attribute = ParseAttributeDefinition(element, pendingComment, globalAttributes);
                    attributes[attribute.Name] = attribute;
                    pendingComment = string.Empty;
                    break;
                default:
                    pendingComment = string.Empty;
                    break;
            }
        }

        return attributes;
    }

    private static AndroidVectorDrawableAttributeSpec ParseAttributeDefinition(
        XElement attributeElement,
        string pendingComment,
        IReadOnlyDictionary<string, AndroidVectorDrawableAttributeSpec> fallbackDefinitions)
    {
        var attributeName = (string?)attributeElement.Attribute("name")
            ?? throw new InvalidOperationException("Encountered <attr> without a name attribute in pinned attrs.xml.");

        fallbackDefinitions.TryGetValue(attributeName, out var fallbackDefinition);

        var formats = ParseFormats((string?)attributeElement.Attribute("format"), fallbackDefinition);
        var enumValues = ParseEnumValues(attributeElement, fallbackDefinition);
        var isHidden =
            CommentContainsHide(pendingComment)
            || fallbackDefinition?.IsHidden == true
            || attributeName.StartsWith("opticalInset", StringComparison.Ordinal);

        return new AndroidVectorDrawableAttributeSpec(attributeName, formats, enumValues, isHidden);
    }

    private static IReadOnlySet<string> ParseFormats(string? rawFormat, AndroidVectorDrawableAttributeSpec? fallbackDefinition)
    {
        var formats = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(rawFormat) == false)
        {
            foreach (var format in rawFormat.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                formats.Add(format);
            }
        }

        if (formats.Count == 0 && fallbackDefinition is not null)
        {
            foreach (var format in fallbackDefinition.Formats)
            {
                formats.Add(format);
            }
        }

        return formats;
    }

    private static IReadOnlySet<string> ParseEnumValues(
        XElement attributeElement,
        AndroidVectorDrawableAttributeSpec? fallbackDefinition)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in attributeElement.Elements())
        {
            if (child.Name.LocalName is not ("enum" or "flag"))
            {
                continue;
            }

            var enumName = (string?)child.Attribute("name");
            if (string.IsNullOrWhiteSpace(enumName) == false)
            {
                values.Add(enumName!);
            }
        }

        if (values.Count == 0 && fallbackDefinition is not null)
        {
            foreach (var enumValue in fallbackDefinition.EnumValues)
            {
                values.Add(enumValue);
            }
        }

        return values;
    }

    private static string AppendComment(string existingComment, string newComment)
    {
        return string.IsNullOrWhiteSpace(existingComment)
            ? newComment
            : string.Concat(existingComment, Environment.NewLine, newComment);
    }

    private static bool CommentContainsHide(string comment)
    {
        return comment.IndexOf("@hide", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetPinnedAttrsPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            "Android",
            $"{PinnedTag}.attrs.xml");
    }

    private static IReadOnlySet<string> CreateSet(params string[] values)
    {
        return new HashSet<string>(values, StringComparer.Ordinal);
    }
}

internal sealed class AndroidVectorDrawableSpecValidator
{
    private static readonly Regex s_dimensionRegex = new(
        @"^[+-]?(?:\d+(?:\.\d+)?|\.\d+)(?:dp|dip|sp|px|pt|pc|in|mm|cm)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly AndroidVectorDrawableSpec _spec;
    private readonly VectorDrawableImplementationContract _contract;

    public AndroidVectorDrawableSpecValidator(AndroidVectorDrawableSpec spec, VectorDrawableImplementationContract contract)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
    }

    public AndroidVectorDrawableValidationResult Validate(string xml, bool allowImplementationExtensions = false)
    {
        using var stringReader = new StringReader(xml ?? throw new ArgumentNullException(nameof(xml)));
        using var reader = XmlReader.Create(
            stringReader,
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            });

        var document = XDocument.Load(reader, LoadOptions.None);
        return Validate(document, allowImplementationExtensions);
    }

    public AndroidVectorDrawableValidationResult Validate(XDocument document, bool allowImplementationExtensions = false)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var errors = new List<string>();
        var root = document.Root;
        if (root is null)
        {
            errors.Add("Document is missing a root element.");
            return new AndroidVectorDrawableValidationResult(errors);
        }

        if (root.Name.LocalName != "vector")
        {
            errors.Add($"Root element must be <vector>, found <{root.Name.LocalName}>.");
            return new AndroidVectorDrawableValidationResult(errors);
        }

        if (root.GetNamespaceOfPrefix("android")?.NamespaceName != AndroidVectorDrawableSpecGenerator.AndroidNamespace)
        {
            errors.Add("Root element is missing the Android VectorDrawable namespace declaration.");
            return new AndroidVectorDrawableValidationResult(errors);
        }

        ValidateElement(root, "vector", errors, allowImplementationExtensions);
        return new AndroidVectorDrawableValidationResult(errors);
    }

    private void ValidateElement(XElement element, string elementName, List<string> errors, bool allowImplementationExtensions)
    {
        if (_spec.Elements.TryGetValue(elementName, out var elementSpec) == false)
        {
            errors.Add($"Unsupported VectorDrawable element <{elementName}>.");
            return;
        }

        foreach (var requiredAttribute in elementSpec.RequiredAttributes)
        {
            if (element.Attribute(XName.Get(requiredAttribute, AndroidVectorDrawableSpecGenerator.AndroidNamespace)) is null)
            {
                errors.Add($"<{elementName}> is missing required android:{requiredAttribute}.");
            }
        }

        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            if (attribute.Name.NamespaceName != AndroidVectorDrawableSpecGenerator.AndroidNamespace)
            {
                errors.Add($"Unsupported attribute '{attribute.Name}' on <{elementName}>.");
                continue;
            }

            if (elementSpec.Attributes.TryGetValue(attribute.Name.LocalName, out var attributeSpec))
            {
                ValidateAttributeValue(elementName, attribute.Name.LocalName, attribute.Value, attributeSpec, errors);
                continue;
            }

            if (allowImplementationExtensions
                && _contract.TryGetRule(elementName, attribute.Name.LocalName, out var extensionRule)
                && extensionRule.Disposition == VectorDrawableContractDisposition.SupportedExtension)
            {
                ValidateExtensionAttributeValue(elementName, attribute.Name.LocalName, attribute.Value, extensionRule, errors);
                continue;
            }

            errors.Add($"Unsupported android:{attribute.Name.LocalName} on <{elementName}>.");
        }

        ValidateElementConstraints(element, elementName, errors);

        foreach (var child in element.Elements())
        {
            var childName = child.Name.LocalName;
            if (elementSpec.AllowedChildren.Contains(childName) == false)
            {
                errors.Add($"<{elementName}> cannot contain <{childName}>.");
                continue;
            }

            ValidateElement(child, childName, errors, allowImplementationExtensions);
        }
    }

    private static void ValidateAttributeValue(
        string elementName,
        string attributeName,
        string value,
        AndroidVectorDrawableAttributeSpec attributeSpec,
        List<string> errors)
    {
        if (IsValidForFormats(value, attributeSpec))
        {
            return;
        }

        errors.Add($"Invalid value '{value}' for android:{attributeName} on <{elementName}>.");
    }

    private static void ValidateExtensionAttributeValue(
        string elementName,
        string attributeName,
        string value,
        VectorDrawableContractRule extensionRule,
        List<string> errors)
    {
        if (extensionRule.AllowedEnumValues.Count > 0)
        {
            if (extensionRule.AllowedEnumValues.Contains(value.Trim()))
            {
                return;
            }

            errors.Add($"Invalid value '{value}' for extension android:{attributeName} on <{elementName}>.");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Invalid empty value for extension android:{attributeName} on <{elementName}>.");
        }
    }

    private static bool IsValidForFormats(string value, AndroidVectorDrawableAttributeSpec attributeSpec)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed.StartsWith("@", StringComparison.Ordinal) || trimmed.StartsWith("?", StringComparison.Ordinal))
        {
            return true;
        }

        if (attributeSpec.EnumValues.Count > 0 && attributeSpec.EnumValues.Contains(trimmed))
        {
            return true;
        }

        if (attributeSpec.Formats.Count == 0)
        {
            return true;
        }

        foreach (var format in attributeSpec.Formats)
        {
            if (MatchesFormat(trimmed, format, attributeSpec))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesFormat(string value, string format, AndroidVectorDrawableAttributeSpec attributeSpec)
    {
        switch (format)
        {
            case "boolean":
                return bool.TryParse(value, out _);
            case "color":
                return IsColorLiteral(value);
            case "dimension":
                return s_dimensionRegex.IsMatch(value);
            case "enum":
            case "flag":
                return attributeSpec.EnumValues.Contains(value);
            case "float":
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            case "fraction":
                return value.EndsWith("%", StringComparison.Ordinal)
                    || value.EndsWith("%p", StringComparison.Ordinal)
                    || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            case "integer":
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "reference":
            case "string":
                return true;
            default:
                return true;
        }
    }

    private static bool IsColorLiteral(string value)
    {
        if (value.StartsWith("#", StringComparison.Ordinal) == false)
        {
            return false;
        }

        var length = value.Length - 1;
        if (length is not (3 or 4 or 6 or 8))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (Uri.IsHexDigit(value[i]) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateElementConstraints(XElement element, string elementName, List<string> errors)
    {
        if (elementName != "vector")
        {
            return;
        }

        ValidatePositiveDimension(element, "width", errors);
        ValidatePositiveDimension(element, "height", errors);
        ValidatePositiveFloat(element, "viewportWidth", errors);
        ValidatePositiveFloat(element, "viewportHeight", errors);
    }

    private static void ValidatePositiveDimension(XElement element, string attributeName, List<string> errors)
    {
        var attribute = element.Attribute(XName.Get(attributeName, AndroidVectorDrawableSpecGenerator.AndroidNamespace));
        if (attribute is null)
        {
            return;
        }

        var trimmed = attribute.Value.Trim();
        if (trimmed.StartsWith("@", StringComparison.Ordinal) || trimmed.StartsWith("?", StringComparison.Ordinal))
        {
            return;
        }

        if (TryParseLeadingFloat(trimmed, out var value) && value > 0f)
        {
            return;
        }

        errors.Add($"<vector> requires android:{attributeName} > 0.");
    }

    private static void ValidatePositiveFloat(XElement element, string attributeName, List<string> errors)
    {
        var attribute = element.Attribute(XName.Get(attributeName, AndroidVectorDrawableSpecGenerator.AndroidNamespace));
        if (attribute is null)
        {
            return;
        }

        var trimmed = attribute.Value.Trim();
        if (trimmed.StartsWith("@", StringComparison.Ordinal) || trimmed.StartsWith("?", StringComparison.Ordinal))
        {
            return;
        }

        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0f)
        {
            return;
        }

        errors.Add($"<vector> requires android:{attributeName} > 0.");
    }

    private static bool TryParseLeadingFloat(string text, out float value)
    {
        var index = 0;
        while (index < text.Length)
        {
            var current = text[index];
            if (char.IsDigit(current)
                || current == '+'
                || current == '-'
                || current == '.'
                || current == 'e'
                || current == 'E')
            {
                index++;
                continue;
            }

            break;
        }

        var numericPart = text.Substring(0, index);
        return float.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}

public enum VectorDrawableContractDisposition
{
    Supported,
    SupportedWithConstraint,
    Unsupported,
    SupportedExtension
}

public sealed record VectorDrawableContractRule(
    string ElementName,
    string AttributeName,
    VectorDrawableContractDisposition Disposition,
    string AcceptedValue,
    string? RejectedValue = null,
    IReadOnlySet<string>? allowedEnumValues = null)
{
    public IReadOnlySet<string> AllowedEnumValues { get; init; } =
        allowedEnumValues ?? new HashSet<string>(StringComparer.Ordinal);
}

internal sealed class VectorDrawableImplementationContract
{
    private const string AndroidNamespace = AndroidVectorDrawableSpecGenerator.AndroidNamespace;

    private readonly Dictionary<string, VectorDrawableContractRule> _rules;

    private VectorDrawableImplementationContract(IEnumerable<VectorDrawableContractRule> rules)
    {
        _rules = rules.ToDictionary(
            static x => CreateKey(x.ElementName, x.AttributeName),
            StringComparer.Ordinal);
    }

    public IEnumerable<VectorDrawableContractRule> Rules => _rules.Values;

    public static VectorDrawableImplementationContract CreateDefault()
    {
        return new VectorDrawableImplementationContract(
        [
            new("vector", "name", VectorDrawableContractDisposition.Supported, "vectorRoot"),
            new("vector", "width", VectorDrawableContractDisposition.Supported, "24dp"),
            new("vector", "height", VectorDrawableContractDisposition.Supported, "24dp"),
            new("vector", "viewportWidth", VectorDrawableContractDisposition.Supported, "24"),
            new("vector", "viewportHeight", VectorDrawableContractDisposition.Supported, "24"),
            new("vector", "alpha", VectorDrawableContractDisposition.Supported, "0.5"),
            new("vector", "tint", VectorDrawableContractDisposition.Unsupported, "#ff336699"),
            new("vector", "tintMode", VectorDrawableContractDisposition.Unsupported, "src_in"),
            new("vector", "autoMirrored", VectorDrawableContractDisposition.SupportedWithConstraint, "false", "true"),

            new("group", "name", VectorDrawableContractDisposition.Supported, "groupOne"),
            new("group", "rotation", VectorDrawableContractDisposition.Supported, "45"),
            new("group", "pivotX", VectorDrawableContractDisposition.Supported, "12"),
            new("group", "pivotY", VectorDrawableContractDisposition.Supported, "12"),
            new("group", "scaleX", VectorDrawableContractDisposition.Supported, "2"),
            new("group", "scaleY", VectorDrawableContractDisposition.Supported, "3"),
            new("group", "translateX", VectorDrawableContractDisposition.Supported, "4"),
            new("group", "translateY", VectorDrawableContractDisposition.Supported, "5"),

            new("path", "name", VectorDrawableContractDisposition.Supported, "shape"),
            new("path", "strokeWidth", VectorDrawableContractDisposition.Supported, "2"),
            new("path", "strokeColor", VectorDrawableContractDisposition.Supported, "#ffcc3300"),
            new("path", "strokeAlpha", VectorDrawableContractDisposition.Supported, "0.5"),
            new("path", "fillColor", VectorDrawableContractDisposition.Supported, "#ff336699"),
            new("path", "fillAlpha", VectorDrawableContractDisposition.Supported, "0.5"),
            new("path", "pathData", VectorDrawableContractDisposition.Supported, "M0,0 L2,0 L2,2 Z"),
            new("path", "trimPathStart", VectorDrawableContractDisposition.Unsupported, "0.1"),
            new("path", "trimPathEnd", VectorDrawableContractDisposition.Unsupported, "0.9"),
            new("path", "trimPathOffset", VectorDrawableContractDisposition.Unsupported, "0.25"),
            new("path", "strokeLineCap", VectorDrawableContractDisposition.Supported, "round"),
            new("path", "strokeLineJoin", VectorDrawableContractDisposition.Supported, "bevel"),
            new("path", "strokeMiterLimit", VectorDrawableContractDisposition.Supported, "4"),
            new("path", "fillType", VectorDrawableContractDisposition.Supported, "evenOdd"),

            new("clip-path", "name", VectorDrawableContractDisposition.Supported, "clipper"),
            new("clip-path", "pathData", VectorDrawableContractDisposition.Supported, "M1,1 L23,1 L23,23 L1,23 Z"),
            new(
                "clip-path",
                "fillType",
                VectorDrawableContractDisposition.SupportedExtension,
                "evenOdd",
                allowedEnumValues: new HashSet<string>(new[] { "nonZero", "evenOdd" }, StringComparer.Ordinal))
        ]);
    }

    public bool TryGetRule(string elementName, string attributeName, out VectorDrawableContractRule rule)
    {
        return _rules.TryGetValue(CreateKey(elementName, attributeName), out rule!);
    }

    public string BuildSampleXml(VectorDrawableContractRule rule, string? overrideValue = null)
    {
        var value = overrideValue ?? rule.AcceptedValue;
        var vector = CreateVectorRoot();

        switch (rule.ElementName)
        {
            case "vector":
                vector.SetAndroidAttribute(rule.AttributeName, value);
                vector.Add(CreatePathElement());
                break;
            case "group":
                {
                    var group = CreateGroupElement();
                    group.SetAndroidAttribute(rule.AttributeName, value);
                    group.Add(CreatePathElement());
                    vector.Add(group);
                    break;
                }
            case "path":
                {
                    var path = CreatePathElement();
                    path.SetAndroidAttribute(rule.AttributeName, value);
                    vector.Add(path);
                    break;
                }
            case "clip-path":
                {
                    var group = CreateGroupElement();
                    var clipPath = CreateClipPathElement();
                    clipPath.SetAndroidAttribute(rule.AttributeName, value);
                    group.Add(clipPath);
                    group.Add(CreatePathElement());
                    vector.Add(group);
                    break;
                }
            default:
                throw new InvalidOperationException($"Unsupported sample element '{rule.ElementName}'.");
        }

        return vector.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement CreateVectorRoot()
    {
        return new XElement(
            "vector",
            new XAttribute(XNamespace.Xmlns + "android", AndroidNamespace),
            new XAttribute(XName.Get("width", AndroidNamespace), "24dp"),
            new XAttribute(XName.Get("height", AndroidNamespace), "24dp"),
            new XAttribute(XName.Get("viewportWidth", AndroidNamespace), "24"),
            new XAttribute(XName.Get("viewportHeight", AndroidNamespace), "24"));
    }

    private static XElement CreateGroupElement()
    {
        return new XElement("group");
    }

    private static XElement CreatePathElement()
    {
        return new XElement(
            "path",
            new XAttribute(XName.Get("fillColor", AndroidNamespace), "#ff336699"),
            new XAttribute(XName.Get("pathData", AndroidNamespace), "M0,0 L2,0 L2,2 Z"));
    }

    private static XElement CreateClipPathElement()
    {
        return new XElement(
            "clip-path",
            new XAttribute(XName.Get("pathData", AndroidNamespace), "M1,1 L23,1 L23,23 L1,23 Z"));
    }

    private static string CreateKey(string elementName, string attributeName)
    {
        return string.Concat(elementName, ":", attributeName);
    }
}

internal static class XElementExtensions
{
    public static void SetAndroidAttribute(this XElement element, string attributeName, string value)
    {
        element.SetAttributeValue(XName.Get(attributeName, AndroidVectorDrawableSpecGenerator.AndroidNamespace), value);
    }
}
