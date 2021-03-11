using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DataAnnotatedModelValidations.Tests
{
    public class ValidatorMiddlewareTests
    {
        [Fact(DisplayName = "InvokeAsync - No Arguments")]
        public async Task InvokeAsyncNoArguments()
        {
            var mockContext = new Mock<IMiddlewareContext>();
            mockContext
                .SetupGet(p => p.Field)
                .Returns(new Mock<IObjectField>().Object);
            var mockFieldDelegate = new Mock<FieldDelegate>();
            var middleware = new ValidatorMiddleware(mockFieldDelegate.Object);

            await middleware.InvokeAsync(mockContext.Object).ConfigureAwait(false);
            mockFieldDelegate.Verify();
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
            var mockContext = new Mock<IMiddlewareContext>();
            mockContext
                .SetupGet(p => p.Field)
                .Returns(mockField.Object);
            mockContext.SetupGet(p => p.Path)
                .Returns(Path.New(new NameString("path")));
            var mockFieldDelegate = new Mock<FieldDelegate>();
            var middleware = new ValidatorMiddleware(mockFieldDelegate.Object);

            await middleware.InvokeAsync(mockContext.Object).ConfigureAwait(false);
            mockFieldDelegate.Verify();

            static IEnumerator<IInputField> MockEnumerator()
            {
                yield return default!;
            }
        }
    }
}