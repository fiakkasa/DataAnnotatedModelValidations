using DataAnnotatedModelValidations.Middleware;

namespace DataAnnotatedModelValidations.Tests.Middleware;

public class ValidatorMiddlewareTests
{
    private readonly ValidatorMiddleware _middleware;
    private readonly IMiddlewareContext _mockContext;
    private readonly FieldDelegate _mockFieldDelegate;

    public ValidatorMiddlewareTests()
    {
        _mockContext = Substitute.For<IMiddlewareContext>();
        _mockFieldDelegate = Substitute.For<FieldDelegate>();
        _middleware = new(_mockFieldDelegate);
        _mockContext.Selection.Field
            .Returns(Substitute.For<IObjectField>());
    }

    [Fact]
    public async Task InvokeAsync_Should_Call_Field_Delegate_When_No_Errors()
    {
        _mockContext.HasErrors.Returns(false);

        await _middleware.InvokeAsync(_mockContext);

        Assert.NotEmpty(_mockFieldDelegate.ReceivedCalls());
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Call_Field_Delegate_When_Errors_Reported()
    {
        _mockContext.HasErrors.Returns(true);

        await _middleware.InvokeAsync(_mockContext);

        Assert.Empty(_mockFieldDelegate.ReceivedCalls());
    }
}
