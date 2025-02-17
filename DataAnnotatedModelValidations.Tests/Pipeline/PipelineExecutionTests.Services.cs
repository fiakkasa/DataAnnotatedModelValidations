namespace DataAnnotatedModelValidations.Tests.Pipeline;

public partial class PipelineExecutionTests
{
    public class MockService
    {
        public string Message { get; } = "Splash!";

        public Sample? Get(string? name) => new()
        {
            Name = name
        };
    }
}
