using Microsoft.Extensions.Compliance.Classification;

namespace Voltflow.Api.Security;

/// <summary>
/// G3: data classification for fields marked [PersonalData] in the Domain layer.
/// Registered in Program.cs with ErasingRedactor so the compliance framework
/// replaces any tagged value with "***" before it reaches a log sink.
/// </summary>
public static class PersonalDataTaxonomy
{
    public static string TaxonomyName => typeof(PersonalDataTaxonomy).FullName!;

    public static DataClassification PrivateData { get; } = new(TaxonomyName, nameof(PrivateData));
}

/// <summary>
/// Apply to ILogger method parameters that carry PII so the compliance framework
/// automatically erases the value before writing to any log sink.
/// Usage: Log("Login {Email}", [PrivateData] string email)
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PrivateDataAttribute : DataClassificationAttribute
{
    public PrivateDataAttribute() : base(PersonalDataTaxonomy.PrivateData) { }
}
