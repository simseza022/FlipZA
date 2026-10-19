namespace FlipZA.Tests;

internal static class Fixture
{
    public static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Fixtures", .. path]));
}
