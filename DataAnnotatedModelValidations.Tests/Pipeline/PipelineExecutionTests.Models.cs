namespace DataAnnotatedModelValidations.Tests.Pipeline;

public partial class PipelineExecutionTests
{
    public record InvalidRecord
    {
        [MaxLength(10)]
        public Optional<string> Text { get; init; }
    }

    public class Sample : IValidatableObject
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        [GraphQLIgnore]
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name?.StartsWith("no-property-name") == true)
            {
                yield return new("no-property-name");
            }

            if (Name?.StartsWith("empty-property-name") == true)
            {
                yield return new("empty-property-name", [string.Empty]);
            }

            if (Name?.StartsWith("null-error-message") == true)
            {
                yield return new(null);
            }

            if (Name?.StartsWith("multiple-property-names") == true)
            {
                yield return new("multiple-property-names",
                [
                    "hello",
                    string.Empty,
                    null!,
                    "world"
                ]);
            }

            if (Name?.StartsWith("message-from-service") == true)
            {
                yield return new(validationContext.GetRequiredService<MockService>().Message);
            }
        }
    }

    public record NestedChild
    {
        [Range(1, 10)]
        public int Count { get; set; }
    }

    public record NestedParent : IValidatableObject
    {
        public NestedChild Child { get; set; } = new();

        public List<NestedChild> Children { get; set; } = [];

        [GraphQLIgnore]
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResultsOfChild = new List<ValidationResult>();

            Validator.TryValidateObject(Child, new(Child, null), validationResultsOfChild, true);

            foreach (var item in validationResultsOfChild)
            {
                yield return new(item.ErrorMessage, item.MemberNames.Prepend(nameof(Child)));
            }

            var validationResultOfChildren = new List<ValidationResult>();

            var index = 0;
            foreach (var item in Children)
            {
                validationResultsOfChild.Clear();
                Validator.TryValidateObject(item, new(item, null), validationResultsOfChild, true);
                validationResultOfChildren.AddRange(
                    validationResultsOfChild.Select(childValidationResult =>
                        new ValidationResult(
                            childValidationResult.ErrorMessage,
                            childValidationResult
                                .MemberNames
                                .Select(memberName => $"{nameof(Children)}:[{index}]:{memberName}")
                                .ToArray()
                        )
                    )
                );
                index++;
            }

            foreach (var item in validationResultOfChildren)
            {
                yield return item;
            }
        }
    }

    public record SampleResponse
    {
        public string Name { get; init; } = string.Empty;
        public string? Info { get; init; }
        public int NumberOfPets { get; init; }
        public int? Age { get; init; }
    }

    public record SampleRecord
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }
    }

    public record SampleRecordWithSynthesizedProperty(
        [property: Required]
        [property: StringLength(100, MinimumLength = 3)]
        string? Name
    );

    [AllStringPropertiesMustBeFilled]
    public record SampleRecordWithClassLevelValidationAttribute
    {
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        [StringLength(100, MinimumLength = 3)]
        public string? Info { get; set; }
    }

    public record SampleRecordWithParameterValidationAttribute
    {
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 3)]
        public string Info { get; set; } = string.Empty;
    }
}
