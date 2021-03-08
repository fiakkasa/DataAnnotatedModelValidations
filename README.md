# DataAnnotatedModelValidations

Data Annotated Model Validation Middleware for HotChocolate

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

## Similar Projects

- https://github.com/VarunSaiTeja/Graph.ArgumentValidator
