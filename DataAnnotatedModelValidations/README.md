# DataAnnotatedModelValidations

Data Annotated Model Validation Middleware for HotChocolate.

The purpose of this Middleware is to provide the same behavior like a ASP.Net controllers where all models would be validated according to the specified Data Annotations or the `IValidatableObject` implementation; in essence it's always on.

In addition individual method arguments can be validated using annotations from `System.ComponentModel.Annotations`.

## Usage

Locate your GraphQL Server registration and append `.AddDataAnnotationsValidator()`

ex.

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // ...
    services
        .AddGraphQLServer()
        .AddDataAnnotationsValidator()
        .AddQueryType<Query>();
    // ...
}
```

### Excluding a model from being validated

To exclude a certain method argument from being validated just add the `IgnoreModelValidation` attribute.

ex.

```csharp
public class Sample
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}

public class Query
{
    public string? GetTextIgnoreValidation([IgnoreModelValidation][MinLength(5)] string? text) => text;

    public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? sample) => sample;
}
```

### Models

In C# one may define models in a multitude of ways.

To help alleviate potential issues consider the following:

#### Standard Classes

```csharp
public class Sample
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}
```

#### Records with declared properties

```csharp
public record Sample
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}
```

#### Records with auto synthesized properties

> The auto-property is initialized to the value of the corresponding primary constructor parameter. Attributes can be applied to the synthesized auto-property and its backing field by using `property:` or `field:` targets for attributes syntactically applied to the corresponding record parameter.

https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/records

So per the documentation and following the guideline, a solution to the challenge mentioned would be adding the `property:` prefix to the validation attribute.

```csharp
public record Sample(
    [property:Required]
    [property:MinLength(3)]
    [property:EmailAddress]
    string? Email
);
```

## Notes

When implementing the `IValidatableObject` interface HotChocolate considers the Validate as a resolver; to avoid getting schema errors said method needs to be ignored.

ex.

```csharp
public class Sample : IValidatableObject
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }

    [GraphQLIgnore]
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        Email?.StartsWith("no-property-name") == true
            ? new[] { new ValidationResult("no-property-name") }
            : Enumerable.Empty<ValidationResult>();
}
```

### Numerical sequences

If you need to support numerical sequences in your validation results, consider adding the names as `FieldName:[index]`.

The field name will be transformed in the error path as `fieldName,_index_`.

```json
{
  "errors": [
    {
      "message": "The field Count must be between 1 and 10.",
      "path": ["sample", "obj", "children", "_2_", "count"],
      "extensions": {
        "code": "DAMV-400",
        "field": "sample",
        "type": "Mutation",
        "specifiedBy": "https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type"
      }
    }
  ]
}
```

If you choose to omit the `:` character and add the names as `FieldName[index]` the field name and the index will be represented as one entry, `fieldName_index_`.

```json
{
  "errors": [
    {
      "message": "The field Count must be between 1 and 10.",
      "path": ["sample", "obj", "children_2_", "count"],
      "extensions": {
        "code": "DAMV-400",
        "field": "sample",
        "type": "Mutation",
        "specifiedBy": "https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type"
      }
    }
  ]
}
```

### Multiple members

If multiple member names are added they will be treated as distinct error messages.

Ex. validation error '"Some validation error!"' was assigned to properties hello and world:

```json
{
  "errors": [
    {
      "message": "Some validation error!",
      "path": ["sample", "obj", "hello"],
      "extensions": {
        "code": "DAMV-400",
        "field": "sample",
        "type": "Query",
        "specifiedBy": "https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type"
      }
    },
    {
      "message": "Some validation error!",
      "path": ["sample", "obj", "world"],
      "extensions": {
        "code": "DAMV-400",
        "field": "sample",
        "type": "Query",
        "specifiedBy": "https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type"
      }
    }
  ],
  "data": {
    "sample": null
  }
}
```

### Property nesting

If there is a need to express a nested relationship of a property and it's parent, consider adding the names separated by a colon as `ParentName:FieldName:[index]`
