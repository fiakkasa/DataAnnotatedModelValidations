using DataAnnotatedModelValidations.Attributes;
using DataAnnotatedModelValidations.TypeInterceptors;

namespace DataAnnotatedModelValidations.Tests.TypeInterceptors;

public class ValidatorTypeInterceptorTests
{
    private readonly ArgumentDefinition _argument;
    private readonly ObjectTypeDefinition _definition;
    private readonly ValidatorTypeInterceptor _interceptor;
    private readonly Mock<ParameterInfo> _mockParameter;
    private readonly Mock<ITypeCompletionContext> _mockTypeCompletionContext = new();

    public ValidatorTypeInterceptorTests()
    {
        _interceptor = new();
        _mockParameter = new();
        _argument = new ArgumentDefinition
        {
            Parameter = _mockParameter.Object
        };
        var objectFieldDefinition = new ObjectFieldDefinition();
        objectFieldDefinition.Arguments.Add(default!);
        objectFieldDefinition.Arguments.Add(_argument);
        var fields = new BindableList<ObjectFieldDefinition>
        {
            objectFieldDefinition
        };
        _definition = new ObjectTypeDefinition();
        _definition.Fields.AddRange(fields);
    }

    private void Act() =>
        _interceptor.OnBeforeCompleteType(
            _mockTypeCompletionContext.Object,
            _definition
        );

    [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - Ignore")]
    public void OnBeforeCompleteTypeWithNullParametersIgnore()
    {
        _mockParameter
            .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
            .Returns(true);

        Act();

        _mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
        Assert.True(_argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
    }

    [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - Attributes")]
    public void OnBeforeCompleteTypeWithNullParametersAttributes()
    {
        _mockParameter
            .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
            .Returns(false);
        _mockParameter
            .SetupGet(p => p.ParameterType)
            .Returns(new Mock<Type>().Object);
        _mockParameter
            .Setup(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()))
            .Returns([new Mock<ValidationAttribute>().Object]);

        Act();

        _mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
        _mockParameter.Verify(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
        Assert.False(_argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
        Assert.True(_argument.ContextData.ContainsKey(nameof(ValidationAttribute)));
    }

    [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - No Attributes")]
    public void OnBeforeCompleteTypeWithNullParametersNoAttributes()
    {
        _mockParameter
            .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
            .Returns(false);
        _mockParameter
            .SetupGet(p => p.ParameterType)
            .Returns(new Mock<Type>().Object);
        _mockParameter
            .Setup(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()))
            .Returns(default(ValidationAttribute[])!);

        Act();

        _mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
        _mockParameter.Verify(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
        Assert.False(_argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
        Assert.False(_argument.ContextData.ContainsKey(nameof(ValidationAttribute)));
    }
}
