using Pkl.Evaluation;

namespace Pkl.Tests.Decoding;

public class VersionCheckTests
{
    [Fact]
    public async Task VersionsBelowMinimumAreRejectedWithClearError()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("uses a shell script as a fake pkl executable");
            return;
        }

        var script = Path.Combine(Path.GetTempPath(), $"fake-pkl-{Guid.NewGuid():N}.sh");
        await File.WriteAllTextAsync(script, "#!/bin/sh\nif [ \"$1\" = \"--version\" ]; then echo 'Pkl 0.25.3 (fake)'; else sleep 30; fi\n", TestContext.Current.CancellationToken);
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        try
        {
            var manager = new EvaluatorManager.EvaluatorManager([script]);
            var ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons()));

            Assert.Contains("0.25.3", ex.Message);
            Assert.Contains("0.26.0", ex.Message);
            try { manager.Close(); } catch { /* fake process; best effort */ }
        }
        finally
        {
            File.Delete(script);
        }
    }
}
