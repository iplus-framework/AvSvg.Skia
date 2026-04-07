using System;
using System.Collections.Generic;
using System.Linq;
using Svg.Model.Services;
using Xunit;

namespace Svg.Model.UnitTests;

public class VectorDrawableAndroidSpecTests
{
    private static readonly AndroidVectorDrawableSpec s_spec = AndroidVectorDrawableSpecGenerator.LoadPinned();
    private static readonly VectorDrawableImplementationContract s_contract = VectorDrawableImplementationContract.CreateDefault();
    private static readonly AndroidVectorDrawableSpecValidator s_validator = new(s_spec, s_contract);

    public static TheoryData<VectorDrawableContractRule> SupportedRuleData =>
        CreateTheoryData(VectorDrawableContractDisposition.Supported);

    public static TheoryData<VectorDrawableContractRule> UnsupportedRuleData =>
        CreateTheoryData(VectorDrawableContractDisposition.Unsupported);

    public static TheoryData<VectorDrawableContractRule> ConstrainedRuleData =>
        CreateTheoryData(VectorDrawableContractDisposition.SupportedWithConstraint);

    public static TheoryData<VectorDrawableContractRule> ExtensionRuleData =>
        CreateTheoryData(VectorDrawableContractDisposition.SupportedExtension);

    [Fact]
    public void PinnedSpec_ParsesExpectedVectorDrawableStyleables()
    {
        Assert.Equal(AndroidVectorDrawableSpecGenerator.PinnedTag, s_spec.PinnedTag);
        Assert.Equal(AndroidVectorDrawableSpecGenerator.PinnedSourceUrl, s_spec.SourceUrl);
        Assert.Equal(
            new[] { "clip-path", "group", "path", "vector" },
            s_spec.Elements.Keys.OrderBy(static x => x, StringComparer.Ordinal));

        Assert.Contains("width", s_spec.Elements["vector"].Attributes.Keys);
        Assert.Contains("rotation", s_spec.Elements["group"].Attributes.Keys);
        Assert.Contains("fillType", s_spec.Elements["path"].Attributes.Keys);
        Assert.DoesNotContain("fillType", s_spec.Elements["clip-path"].Attributes.Keys);
    }

    [Fact]
    public void PinnedSpec_ClassifiesEveryNonHiddenAndroidAttribute()
    {
        var unclassified = new List<string>();

        foreach (var element in s_spec.Elements.Values)
        {
            foreach (var attribute in element.Attributes.Values.Where(static x => x.IsHidden == false))
            {
                if (s_contract.TryGetRule(element.ElementName, attribute.Name, out _))
                {
                    continue;
                }

                unclassified.Add($"{element.ElementName}.android:{attribute.Name}");
            }
        }

        Assert.True(
            unclassified.Count == 0,
            $"Unclassified Android VectorDrawable attributes:{Environment.NewLine}{string.Join(Environment.NewLine, unclassified)}");
    }

    [Fact]
    public void PinnedSpec_OnlyAllowsExplicitExtensionsOutsideAndroidContract()
    {
        var unexpected = s_contract.Rules
            .Where(static x => x.Disposition != VectorDrawableContractDisposition.SupportedExtension)
            .Where(x => s_spec.Elements.TryGetValue(x.ElementName, out var element) == false || element.Attributes.ContainsKey(x.AttributeName) == false)
            .Select(static x => $"{x.ElementName}.android:{x.AttributeName}")
            .ToArray();

        Assert.Empty(unexpected);

        var extensions = s_contract.Rules
            .Where(static x => x.Disposition == VectorDrawableContractDisposition.SupportedExtension)
            .Select(static x => $"{x.ElementName}.android:{x.AttributeName}")
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "clip-path.android:fillType" }, extensions);
    }

    [Theory]
    [MemberData(nameof(SupportedRuleData))]
    public void FromVectorDrawable_AcceptsSupportedAndroidAttributes(VectorDrawableContractRule rule)
    {
        var xml = s_contract.BuildSampleXml(rule);
        var validation = s_validator.Validate(xml);

        Assert.True(validation.IsValid, validation.ToString());
        Assert.NotNull(SvgService.FromVectorDrawable(xml));
    }

    [Theory]
    [MemberData(nameof(UnsupportedRuleData))]
    public void FromVectorDrawable_RejectsExplicitUnsupportedAndroidAttributes(VectorDrawableContractRule rule)
    {
        var xml = s_contract.BuildSampleXml(rule);
        var validation = s_validator.Validate(xml);

        Assert.True(validation.IsValid, validation.ToString());
        Assert.Throws<NotSupportedException>(() => SvgService.FromVectorDrawable(xml));
    }

    [Theory]
    [MemberData(nameof(ConstrainedRuleData))]
    public void FromVectorDrawable_AppliesConditionalAttributeRules(VectorDrawableContractRule rule)
    {
        var acceptedXml = s_contract.BuildSampleXml(rule);
        var acceptedValidation = s_validator.Validate(acceptedXml);

        Assert.True(acceptedValidation.IsValid, acceptedValidation.ToString());
        Assert.NotNull(SvgService.FromVectorDrawable(acceptedXml));

        Assert.NotNull(rule.RejectedValue);
        var rejectedXml = s_contract.BuildSampleXml(rule, rule.RejectedValue);
        var rejectedValidation = s_validator.Validate(rejectedXml);

        Assert.True(rejectedValidation.IsValid, rejectedValidation.ToString());
        Assert.Throws<NotSupportedException>(() => SvgService.FromVectorDrawable(rejectedXml));
    }

    [Theory]
    [MemberData(nameof(ExtensionRuleData))]
    public void Validator_RequiresExtensionsToBeExplicit(VectorDrawableContractRule rule)
    {
        var xml = s_contract.BuildSampleXml(rule);
        var strictValidation = s_validator.Validate(xml);
        var extensionValidation = s_validator.Validate(xml, allowImplementationExtensions: true);

        Assert.False(strictValidation.IsValid);
        Assert.Contains(strictValidation.Errors, static x => x.Contains("android:fillType", StringComparison.Ordinal));
        Assert.True(extensionValidation.IsValid, extensionValidation.ToString());
        Assert.NotNull(SvgService.FromVectorDrawable(xml));
    }

    [Fact]
    public void Validator_RejectsUnknownAndroidAttribute()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="24dp"
                android:viewportWidth="24"
                android:viewportHeight="24">
                <path
                    android:unknown="1"
                    android:fillColor="#ff336699"
                    android:pathData="M0,0 L2,0 L2,2 Z" />
            </vector>
            """;

        var validation = s_validator.Validate(xml);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, static x => x.Contains("android:unknown", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_RejectsInvalidElementNesting()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="24dp"
                android:viewportWidth="24"
                android:viewportHeight="24">
                <path
                    android:fillColor="#ff336699"
                    android:pathData="M0,0 L2,0 L2,2 Z">
                    <group />
                </path>
            </vector>
            """;

        var validation = s_validator.Validate(xml);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, static x => x.Contains("<path> cannot contain <group>", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("width", "0dp")]
    [InlineData("height", "-1dp")]
    [InlineData("viewportWidth", "0")]
    [InlineData("viewportHeight", "-1")]
    public void Validator_RejectsNonPositiveRequiredVectorMetrics(string attributeName, string value)
    {
        var width = attributeName == "width" ? value : "24dp";
        var height = attributeName == "height" ? value : "24dp";
        var viewportWidth = attributeName == "viewportWidth" ? value : "24";
        var viewportHeight = attributeName == "viewportHeight" ? value : "24";

        var xml = $"""
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="{width}"
                android:height="{height}"
                android:viewportWidth="{viewportWidth}"
                android:viewportHeight="{viewportHeight}">
                <path
                    android:fillColor="#ff336699"
                    android:pathData="M0,0 L2,0 L2,2 Z" />
            </vector>
            """;

        var validation = s_validator.Validate(xml);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Contains($":{attributeName} > 0", StringComparison.Ordinal));
    }

    private static TheoryData<VectorDrawableContractRule> CreateTheoryData(VectorDrawableContractDisposition disposition)
    {
        var data = new TheoryData<VectorDrawableContractRule>();
        foreach (var rule in s_contract.Rules.Where(x => x.Disposition == disposition))
        {
            data.Add(rule);
        }

        return data;
    }
}
