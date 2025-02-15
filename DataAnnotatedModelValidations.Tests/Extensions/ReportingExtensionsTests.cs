using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests.Extensions;

public class ReportingExtensionsTests
{
    private readonly IInputField _argument;

    public ReportingExtensionsTests()
    {
        _argument = Substitute.For<IInputField>();
        _argument.Name.Returns("Name");
    }

    [Fact]
    public void GetNormalizedMemberName_Should_Normalize_Value() =>
        Assert.Equal("helloWorld_0_", "HelloWorld[0]".GetNormalizedMemberName());

    [Fact]
    public void ToTokenizedMemberNames_Should_Tokenize_Value() =>
        Assert.Equal(
            [
                "hello",
                "world"
            ],
            "hello:world".ToTokenizedMemberNames()
        );

    [Fact]
    public void ToComposedMemberNames_Should_Enumerate_Name_When_MemberName_Is_Null() =>
        Assert.Equal(["Name"], _argument.ToComposedMemberNames(default, default));

    [Fact]
    public void ToComposedMemberNames_Should_Enumerate_Name_When_MemberName_Is_Blank() =>
        Assert.Equal(["Name"], _argument.ToComposedMemberNames(" ", default));

    [Fact]
    public void ToComposedMemberNames_Should_Enumerate_MemberName_And_Name_When_ValueValidation_Is_Not_True() =>
        Assert.Equal(
            [
                "Name",
                "hello",
                "world_0_"
            ],
            _argument.ToComposedMemberNames("Hello:World[0]", default)
        );

    [Fact]
    public void ToComposedMemberNames_Should_Enumerate_MemberName_When_ValueValidation_Is_True() =>
        Assert.Equal(
            [
                "hello",
                "world_0_"
            ],
            _argument.ToComposedMemberNames("Hello:World[0]", true)
        );

    [Fact]
    public void ToArgumentPath_Should_Produce_Path()
    {
        var result = new[] { "hello" }.ToArgumentPath(["world"]);

        Assert.Equal(
            Path.Root.Append("hello").Append("world"),
            result
        );
    }

    [Fact]
    public void ReportError_Should_Report_With_Code_Path_And_Message_When_Message_Not_Null()
    {
        var context = Substitute.For<IMiddlewareContext>();

        context.ReportError(
            _argument,
            ["hello"],
            default,
            "world",
            default
        );

        var receivedArguments = context.ReceivedCalls().SelectMany(x => x.GetArguments()).ToArray();

        Assert.Single(
            receivedArguments,
            x =>
                x is Error
                {
                    Code: ReportingConsts.GenericErrorCode,
                    Message: "world",
                    Path: { Length: 2 } p
                }
                && p.Equals(Path.Root.Append("hello").Append("Name"))
        );
    }

    [Fact]
    public void ReportError_Should_Report_With_Code_Path_And_Default_Message_When_Message_Null()
    {
        var context = Substitute.For<IMiddlewareContext>();

        context.ReportError(
            _argument,
            ["hello"],
            default,
            default,
            "member"
        );

        var receivedArguments = context.ReceivedCalls().SelectMany(x => x.GetArguments()).ToArray();

        Assert.Single(
            receivedArguments,
            x => x is Error
                 {
                     Code: ReportingConsts.GenericErrorCode,
                     Message: ReportingConsts.GenericErrorMessage,
                     Path: { Length: 3 } p
                 }
                 && p.Equals(Path.Root.Append("hello").Append("Name").Append("member"))
        );
    }
}
