using JetBrains.Annotations;

namespace Usenet.Util;

/// <summary>
/// Represents a validation result.
/// </summary>
[PublicAPI]
public class ValidationResult
{
    /// <summary>
    /// A collection of <see cref="ValidationFailure"/> objects.
    /// </summary>
    public IReadOnlyList<ValidationFailure> Failures { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="failures">A collection of <see cref="ValidationFailure"/> objects.</param>
    internal ValidationResult(IReadOnlyList<ValidationFailure> failures) => Failures = failures;

    /// <summary>
    /// A property indicating whether the validation result is valid or not.
    /// </summary>
    /// <returns>true if the validation result contains no validation failures; otherwise false</returns>
    public bool IsValid => Failures.Count == 0;
}
