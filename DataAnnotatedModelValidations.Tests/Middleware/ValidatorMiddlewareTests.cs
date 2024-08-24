using DataAnnotatedModelValidations.Middleware;

namespace DataAnnotatedModelValidations.Tests.Middleware;

public class ValidatorMiddlewareTests
{
    private readonly ValidatorMiddleware _middleware;
    private readonly Mock<IMiddlewareContext> _mockContext;
    private readonly Mock<FieldDelegate> _mockFieldDelegate;

    public ValidatorMiddlewareTests()
    {
        _mockFieldDelegate = new();
        _mockContext = new();
        _middleware = new(_mockFieldDelegate.Object);
    }

    [Fact(DisplayName = "InvokeAsync - No Arguments")]
    public async Task InvokeAsyncNoArguments()
    {
        _mockContext
            .SetupGet(p => p.Selection.Field)
            .Returns(new Mock<IObjectField>().Object);

        await _middleware.InvokeAsync(_mockContext.Object);
        _mockFieldDelegate.Verify();
    }

    [Fact(DisplayName = "InvokeAsync - Null Arguments")]
    public async Task InvokeAsyncNullArguments()
    {
        var mockFieldCollection = new Mock<IFieldCollection<IInputField>>();
        mockFieldCollection
            .Setup(m => m.GetEnumerator())
            .Returns(MockEnumerator);
        mockFieldCollection
            .SetupGet(p => p.Count)
            .Returns(1);
        var mockField = new Mock<IObjectField>();
        mockField
            .SetupGet(p => p.Arguments)
            .Returns(mockFieldCollection.Object);
        _mockContext
            .SetupGet(p => p.Selection.Field)
            .Returns(mockField.Object);
        _mockContext
            .SetupGet(p => p.Path)
            .Returns(Path.Root.Append("path"));

        await _middleware.InvokeAsync(_mockContext.Object);
        _mockFieldDelegate.Verify();

        static IEnumerator<IInputField> MockEnumerator()
        {
            yield return default!;
        }
    }
}
