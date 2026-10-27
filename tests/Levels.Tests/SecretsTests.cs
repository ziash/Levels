namespace Levels.Tests;

public class SecretsTests
{
    [Fact]
    public void Parses_keys_comments_quotes_and_export()
    {
        var keys = Secrets.Parse([
            "# Levels keys",
            "",
            "TRADIER_TOKEN=abc123",
            "export TRADIER_BASE=\"https://api.tradier.com/v1/\"",
            "BROKEN LINE",
            "EMPTY=",
        ]);

        Assert.Equal("abc123", keys["TRADIER_TOKEN"]);
        Assert.Equal("https://api.tradier.com/v1/", keys["TRADIER_BASE"]);
        Assert.Equal("", keys["EMPTY"]);
        Assert.Equal(3, keys.Count);
    }

    [Fact]
    public void Missing_file_gives_no_keys() =>
        Assert.Empty(Secrets.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "secrets.env")));
}
