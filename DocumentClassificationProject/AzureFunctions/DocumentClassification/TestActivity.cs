using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocumentClassification;

public class TestActivity
{
    private readonly ILogger<TestActivity> _logger;

    public TestActivity(ILogger<TestActivity> logger)
    {
        _logger = logger;
    }

    [Function(nameof(TestActivity))]
    public async Task<string> Run([ActivityTrigger] string input)
    {
        _logger.LogInformation($"TestActivity running with input: {input}");
        return "Success";
    }
}
