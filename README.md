# DataAnnotatedModelValidations

Data Annotated Model Validation Middleware for HotChocolate.

The purpose of this Middleware is to provide the same behavior like a ASP.Net controllers where all models would be validated according to the specified Data Annotations or the `IValidatableObject` implementation; in essence it's always on.

In addition individual method arguments can be validated using annotations from `System.ComponentModel.Annotations`.

[Nuget](https://www.nuget.org/packages/DataAnnotatedModelValidations/)

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

To exlude a certain method argument from being validated just add the `IgnoreModelValidation` attribute.

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

## Similar Projects

- https://github.com/VarunSaiTeja/Graph.ArgumentValidator
