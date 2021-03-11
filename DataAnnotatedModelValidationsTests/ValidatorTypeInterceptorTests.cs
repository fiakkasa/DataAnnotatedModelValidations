using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Xunit;

namespace DataAnnotatedModelValidations.Tests
{
    public class ValidatorTypeInterceptorTests
    {
        private readonly ValidatorTypeInterceptor interceptor;
        private readonly Mock<ParameterInfo> mockParameter;
        private readonly ObjectTypeDefinition definition;
        private readonly ArgumentDefinition argument;
        private readonly Mock<ITypeCompletionContext> mockTypeCompletionContext = new();
        private readonly Dictionary<string, object?> contextData = new();

        public ValidatorTypeInterceptorTests()
        {
            interceptor = new();
            mockParameter = new();
            argument = new ArgumentDefinition
            {
                Parameter = mockParameter.Object
            };
            var objectFieldDefinition = new ObjectFieldDefinition();
            objectFieldDefinition.Arguments.Add(default!);
            objectFieldDefinition.Arguments.Add(argument);
            var fields = new BindableList<ObjectFieldDefinition> { objectFieldDefinition };
            definition = new ObjectTypeDefinition();
            definition.Fields.AddRange(fields);
        }

        private void Act() =>
            interceptor.OnBeforeCompleteType(
                mockTypeCompletionContext.Object,
                definition,
                contextData
            );

        [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - Ignore")]
        public void OnBeforeCompleteTypeWithNullParametersIgnore()
        {
            mockParameter
                .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
                .Returns(true);

            Act();

            mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
            Assert.True(argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
        }

        [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - Attributes")]
        public void OnBeforeCompleteTypeWithNullParametersAttributes()
        {
            mockParameter
                .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
                .Returns(false);
            mockParameter
                .SetupGet(p => p.ParameterType)
                .Returns(new Mock<Type>().Object);
            mockParameter
                .Setup(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()))
                .Returns(new[] { new Mock<ValidationAttribute>().Object });

            Act();

            mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
            mockParameter.Verify(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
            Assert.False(argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
            Assert.True(argument.ContextData.ContainsKey(nameof(ValidationAttribute)));
        }

        [Fact(DisplayName = "OnBeforeCompleteType - With Null Parameters - No Attributes")]
        public void OnBeforeCompleteTypeWithNullParametersNoAttributes()
        {
            mockParameter
                .Setup(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()))
                .Returns(false);
            mockParameter
                .SetupGet(p => p.ParameterType)
                .Returns(new Mock<Type>().Object);
            mockParameter
                .Setup(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()))
                .Returns(default(ValidationAttribute[])!);

            Act();

            mockParameter.Verify(m => m.IsDefined(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
            mockParameter.Verify(m => m.GetCustomAttributes(It.IsAny<Type>(), It.IsAny<bool>()), Times.Once);
            Assert.False(argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute)));
            Assert.False(argument.ContextData.ContainsKey(nameof(ValidationAttribute)));
        }
    }
}