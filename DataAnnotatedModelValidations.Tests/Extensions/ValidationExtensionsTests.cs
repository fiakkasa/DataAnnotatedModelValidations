using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests.Extensions;

public class ValidationExtensionsTests
{
    private readonly IMiddlewareContext _mockContext;

    public ValidationExtensionsTests()
    {
        _mockContext = Substitute.For<IMiddlewareContext>();
        _mockContext.Path.Returns(Path.Root.Append("path"));
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Not_Set()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(false);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_Arguments_Is_Null()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        _mockContext.Selection.Field.Arguments.Returns(default(IFieldCollection<IInputField>?)!);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_No_Arguments_Present()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        _mockContext.Selection.Field.Arguments.Returns(FieldCollection<IInputField>.Empty);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_All_Arguments_Are_Null()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var collection = Substitute.For<IFieldCollection<IInputField>>();
        using var collectionEnumerator = collection.GetEnumerator();
        using var mockEnumerator = MockEnumerator(null!, null!, null!).GetEnumerator();
        collectionEnumerator.Returns(mockEnumerator);
        collection.Count.Returns(3);
        _mockContext.Selection.Field.Arguments.Returns(collection);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_No_Arguments_Has_ContextKey_Set()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var argument = Substitute.For<IInputField>();
        argument.Name.Returns("sampleArgument");
        argument.ContextData.ContainsKey(Arg.Any<string>()).Returns(false);
        var collection = Substitute.For<IFieldCollection<IInputField>>();
        using var collectionEnumerator = collection.GetEnumerator();
        using var mockEnumerator = MockEnumerator(argument).GetEnumerator();
        collectionEnumerator.Returns(mockEnumerator);
        collection.Count.Returns(1);
        _mockContext.Selection.Field.Arguments.Returns(collection);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_Context_Returns_Null_For_Argument()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var argument = Substitute.For<IInputField>();
        argument.Name.Returns("sampleArgument");
        argument.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var collection = Substitute.For<IFieldCollection<IInputField>>();
        using var collectionEnumerator = collection.GetEnumerator();
        using var mockEnumerator = MockEnumerator(argument).GetEnumerator();
        collectionEnumerator.Returns(mockEnumerator);
        collection.Count.Returns(1);
        _mockContext.Selection.Field.Arguments.Returns(collection);
        _mockContext.ArgumentValue<object>(Arg.Any<string>()).Returns(null);

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }


    [Fact]
    public void ValidateInputs_Should_Not_Report_Error_When_Field_ContextKey_Is_Set_And_Arguments_Has_ContextKey_Set_With_Valid_Data()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var argument = Substitute.For<IInputField>();
        argument.Name.Returns("sampleArgument");
        argument.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var collection = Substitute.For<IFieldCollection<IInputField>>();
        using var collectionEnumerator = collection.GetEnumerator();
        using var mockEnumerator = MockEnumerator(argument).GetEnumerator();
        collectionEnumerator.Returns(mockEnumerator);
        collection.Count.Returns(1);
        _mockContext.Selection.Field.Arguments.Returns(collection);
        _mockContext.ArgumentValue<object>(Arg.Any<string>()).Returns(new SampleRecord("Name"));

        _mockContext.ValidateInputs();

        Assert.DoesNotContain(_mockContext.ReceivedCalls(), x => x.GetMethodInfo().Name == "ReportError");
    }

    [Fact]
    public void ValidateInputs_Should_Report_Error_When_Field_ContextKey_Is_Set_And_Arguments_Has_ContextKey_Set_With_Invalid_Data()
    {
        _mockContext.Selection.Field.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var argument = Substitute.For<IInputField>();
        argument.Name.Returns("sampleArgument");
        argument.ContextData.ContainsKey(Arg.Any<string>()).Returns(true);
        var collection = Substitute.For<IFieldCollection<IInputField>>();
        using var collectionEnumerator = collection.GetEnumerator();
        using var mockEnumerator = MockEnumerator(argument).GetEnumerator();
        collectionEnumerator.Returns(mockEnumerator);
        collection.Count.Returns(1);
        _mockContext.Selection.Field.Arguments.Returns(collection);
        _mockContext.ArgumentValue<object>(Arg.Any<string>()).Returns(new SampleRecord());

        _mockContext.ValidateInputs();

        Assert.Contains(_mockContext.ReceivedCalls(),
            x =>
                x.GetMethodInfo().Name == "ReportError"
                && x.GetArguments() is [IError { Code: ReportingConsts.GenericErrorCode }]
        );
    }

    private static IEnumerable<IInputField> MockEnumerator(params IInputField?[] items)
    {
        foreach (var item in items)
        {
            yield return item!;
        }
    }

    public record SampleRecord(
        [property: Required]
        string? Name = default
    );
}
