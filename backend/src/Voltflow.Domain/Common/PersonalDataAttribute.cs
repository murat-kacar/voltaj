namespace Voltflow.Domain.Common;

/// <summary>Marks a property that contains personal data (PII). G3: must not appear unmasked in logs or error output.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class PersonalDataAttribute : Attribute;
