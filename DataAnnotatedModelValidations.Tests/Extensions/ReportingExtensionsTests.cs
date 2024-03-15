using System.Collections.Generic;
using System.Linq;
using DataAnnotatedModelValidations.Extensions;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using NSubstitute;

namespace DataAnnotatedModelValidations.Tests;

public class ReportingExtensionsTests
{
    private readonly IInputField _argument;

    public ReportingExtensionsTests()
    {
        _argument = Substitute.For<IInputField>();
        _argument.Name.Returns("Name");
    }

    [Fact]
    public void GetNormalizedMemberName_Normalizes_Value() =>
        Assert.Equal("helloWorld_0_", "HelloWorld[0]".GetNormalizedMemberName());

    [Fact]
    public void ToTokenizedMemberNames_Tokenizes_Value() =>
        Assert.Equal(
            new[] { "hello", "world" },
            "hello:world".ToTokenizedMemberNames()
        );

    [Fact]
    public void ToComposedMemberNames_Enumerates_Name_When_MemberName_Is_Null() =>
        Assert.Equal(new[] { "Name" }, _argument.ToComposedMemberNames(default, default));

    [Fact]
    public void ToComposedMemberNames_Enumerates_Name_When_MemberName_Is_Blank() =>
        Assert.Equal(new[] { "Name" }, _argument.ToComposedMemberNames(" ", default));

    [Fact]
    public void ToComposedMemberNames_Enumerates_MemberName_And_Name_When_ValueValidation_Is_Not_True() =>
        Assert.Equal(new[] { "Name", "hello", "world_0_" }, _argument.ToComposedMemberNames("Hello:World[0]", default));

    [Fact]
    public void ToComposedMemberNames_Enumerates_MemberName_When_ValueValidation_Is_True() =>
        Assert.Equal(new[] { "hello", "world_0_" }, _argument.ToComposedMemberNames("Hello:World[0]", true));

    [Fact]
    public void ToArgumentPath_Produces_Path()
    {
        var result = new List<string> { "hello" }.ToArgumentPath(new[] { "world" });

        Assert.Equal(
            Path.Root.Append("hello").Append("world"),
            result
        );
    }

    [Fact]
    public void ReportError_With_Code_Path_And_Message_When_Message_Not_Null()
    {
        var context = Substitute.For<IMiddlewareContext>();

        context.ReportError(
            _argument,
            new [] { "hello" },
            default,
            "world",
            default
        );

        var receivedArguments = context.ReceivedCalls().SelectMany(x => x.GetArguments()).ToArray();

        Assert.Single(
            receivedArguments,
            x => x is Error
            {
                Code: "DAMV-400",
                Message: "world",
                Path: { Length: 2 } p
            }
            && p.Equals(Path.Root.Append("hello").Append("Name"))
        );
    }

    [Fact]
    public void ReportError_With_Code_Path_And_Default_Message_When_Message_Null()
    {
        var context = Substitute.For<IMiddlewareContext>();

        context.ReportError(
            _argument,
            new [] { "hello" },
            default,
            default,
            "member"
        );

        var receivedArguments = context.ReceivedCalls().SelectMany(x => x.GetArguments()).ToArray();

        Assert.Single(
            receivedArguments,
            x => x is Error
            {
                Code: "DAMV-400",
                Message: "Unspecified Error",
                Path: { Length: 3 } p
            }
            && p.Equals(Path.Root.Append("hello").Append("Name").Append("member"))
        );
    }
}
