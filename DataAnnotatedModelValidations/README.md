# DataAnnotatedModelValidations

Data Annotated Model Validation Middleware for HotChocolate.

The purpose of this Middleware is to provide the same behavior as ASP.Net controllers, where all models would be
validated according to the specified Data Annotations or the `IValidatableObject` implementation; in essence it's always
on.

In addition, individual method arguments can be validated using annotations from `System.ComponentModel.Annotations`.

[Nuget](https://www.nuget.org/packages/DataAnnotatedModelValidations/)

## Compatibility

### Current Releases

| HotChocolate Version | DataAnnotatedModelValidations Version | .NET Version  |
| -------------------- | ------------------------------------- | ------------- |
| 15.1.11 or higher    | 9.0.0                                 | .NET 8, 9, 10 |
| 15.0.3 or higher     | 8.1.2                                 | .NET 8, 9     |
| 15.0.3 or higher     | 8.1.1                                 | .NET 8, 9     |
| 15.0.3 or higher     | 8.1.0                                 | .NET 8, 9     |
| 15.0.3 or higher     | 8.0.1                                 | .NET 8, 9     |
| 15.0.3 or higher     | 8.0.0                                 | .NET 8, 9     |
| 15.0.3 or higher     | 7.0.0                                 | .NET 8, 9     |
| 14.3.0 or higher     | 6.3.0                                 | .NET 8, 9     |
| 14.2.0 or higher     | 6.2.0                                 | .NET 8, 9     |
| 14.1.0 or higher     | 6.1.0                                 | .NET 8, 9     |
| 14.0.0 or higher     | 6.0.0                                 | .NET 8        |

### Past Releases

| HotChocolate Version | Last DataAnnotatedModelValidations Version | .NET Version |
| -------------------- | ------------------------------------------ | ------------ |
| 13.9.0 or higher     | 5.2.0                                      | .NET 6, 7, 8 |
| 13.7.0 or higher     | 4.2.0                                      | .NET 6, 7    |
| 12.16.0 or higher    | 3.0.1                                      | .NET 7       |
| 12.4.1 or higher     | 2.1.4                                      | .NET 6       |

📝For more information please visit https://www.nuget.org/packages/DataAnnotatedModelValidations/#versions-body-tab

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

#### Standard Classes or Records with declared properties

```csharp
public class Sample
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}

public record SampleRecord
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}
```

#### Standard Classes or Records with declared properties using top level attributes

```csharp
[MyCustomObjectValidator]
public class Sample
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}

[MyCustomObjectValidator]
public record SampleRecord
{
    [Required]
    [MinLength(3)]
    [EmailAddress]
    public string? Email { get; set; }
}
```

#### Standard Classes or Records with inline parameter level attributes

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
    public Sample? GetSampleWithParameterLevelAttribute([MyCustomObjectValidator] Sample? sample) => sample;
}
```

#### Records with auto synthesized properties

> The auto-property is initialized to the value of the corresponding primary constructor parameter. Attributes can be
> applied to the synthesized auto-property and its backing field by using `property:` or `field:` targets for attributes
> syntactically applied to the corresponding record parameter.

https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/records

So per the documentation and following the guideline, a solution to the challenge mentioned would be adding the
`property:` prefix to the validation attribute.

```csharp
public record Sample(
    [property:Required]
    [property:MinLength(3)]
    [property:EmailAddress]
    string? Email
);
```

## Notes

When implementing the `IValidatableObject` interface HotChocolate considers the Validate as a resolver; to avoid getting
schema errors said method needs to be ignored.

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
      "path": [
        "sample",
        "obj",
        "children",
        "_2_",
        "count"
      ],
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

If you choose to omit the `:` character and add the names as `FieldName[index]` the field name and the index will be
represented as one entry, `fieldName_index_`.

```json
{
  "errors": [
    {
      "message": "The field Count must be between 1 and 10.",
      "path": [
        "sample",
        "obj",
        "children_2_",
        "count"
      ],
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
      "path": [
        "sample",
        "obj",
        "hello"
      ],
      "extensions": {
        "code": "DAMV-400",
        "field": "sample",
        "type": "Query",
        "specifiedBy": "https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type"
      }
    },
    {
      "message": "Some validation error!",
      "path": [
        "sample",
        "obj",
        "world"
      ],
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

If there is a need to express a nested relationship of a property and it's parent, consider adding the names separated
by a colon as `ParentName:FieldName:[index]`

## Similar Projects

- https://github.com/VarunSaiTeja/Graph.ArgumentValidator
