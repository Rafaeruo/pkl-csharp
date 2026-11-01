using Pkl.Evaluation;

namespace Pkl.Tests.Decoding;

public class VersionCheckTests
{
    [Fact]
    public async Task VersionsBelowMinimumAreRejectedWithClearError()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("uses sh as a fake pkl executable");
            return;
        }

        // A fake pkl: `<cmd> --version` prints a version, anything else (`server`) just idles briefly.
        // It is an inline script rather than a file, as executing a file that was just written races with
        // other tests forking processes on Linux ("Text file busy"). The manager appends its own
        // arguments after the script, where sh exposes them as $0.
        var fakePkl = new[]
        {
            "/bin/sh", "-c", "\"case $0 in --version) echo Pkl 0.25.3 fake;; *) sleep 2;; esac\"",
        };

        var manager = new EvaluatorManager.EvaluatorManager(fakePkl);
        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons()));

        Assert.Contains("0.25.3", ex.Message);
        Assert.Contains("0.26.0", ex.Message);
        try { manager.Close(); } catch { /* fake process; best effort */ }
    }
}
