namespace Levels.Tests;

public class SecretsTests
{
    [Fact]
    public void Parses_keys_comments_quotes_and_export()
    {
        var keys = Secrets.Parse([
            "# Levels keys",
            "",
            "ALPHAVANTAGE_KEY=abc123",
            "export OTHER=\"https://example.com/v1/\"",
            "BROKEN LINE",
            "EMPTY=",
        ]);

        Assert.Equal("abc123", keys["ALPHAVANTAGE_KEY"]);
        Assert.Equal("https://example.com/v1/", keys["OTHER"]);
        Assert.Equal("", keys["EMPTY"]);
        Assert.Equal(3, keys.Count);
    }

    [Fact]
    public void Missing_file_gives_no_keys() =>
        Assert.Empty(Secrets.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "secrets.env")));
}
