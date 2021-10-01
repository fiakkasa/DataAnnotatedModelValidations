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
        private readonly Mock<FieldDelegate> mockFieldDelegate;
        private readonly Mock<IMiddlewareContext> mockContext;
        private readonly ValidatorMiddleware middleware;

        public ValidatorMiddlewareTests()
        {
            mockFieldDelegate = new();
            mockContext = new();
            middleware = new(mockFieldDelegate.Object);
        }

        [Fact(DisplayName = "InvokeAsync - No Arguments")]
        public async Task InvokeAsyncNoArguments()
        {
            mockContext
                .SetupGet(p => p.Selection.Field)
                .Returns(new Mock<IObjectField>().Object);

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
            mockContext
                .SetupGet(p => p.Selection.Field)
                .Returns(mockField.Object);
            mockContext.SetupGet(p => p.Path)
                .Returns(Path.New(new NameString("path")));

            await middleware.InvokeAsync(mockContext.Object).ConfigureAwait(false);
            mockFieldDelegate.Verify();

            static IEnumerator<IInputField> MockEnumerator()
            {
                yield return default!;
            }
        }
    }
}