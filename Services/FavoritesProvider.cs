using System.Text;

namespace FloatingToolbar.Services;

/// <summary>
/// Builds the favourites fly-out from the user's Windows Favorites folder (.url files only).
/// </summary>
public static class FavoritesProvider
{
    public sealed record Favorite(string Name, string Url);

    public static List<Favorite> GetFavorites(int maximum = 25)
    {
        var favorites = new List<Favorite>();
        try
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Collect(directory, favorites, 0, maximum);
            }
        }
        catch (Exception)
        {
            // A missing or locked favourites folder simply yields an empty fly-out.
        }

        return favorites;
    }

    private static void Collect(string directory, List<Favorite> favorites, int depth, int maximum)
    {
        if (depth > 2 || favorites.Count >= maximum) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.url"))
            {
                if (favorites.Count >= maximum) return;

                try
                {
                    string? url = null;
                    foreach (var line in File.ReadLines(file, Encoding.Default))
                    {
                        if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                        {
                            url = line[4..].Trim();
                            break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        favorites.Add(new Favorite(Path.GetFileNameWithoutExtension(file), url));
                    }
                }
                catch (Exception)
                {
                    // Skip unreadable shortcut files.
                }
            }

            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                if (favorites.Count >= maximum) return;
                Collect(subDirectory, favorites, depth + 1, maximum);
            }
        }
        catch (Exception)
        {
            // Ignore access errors on individual folders.
        }
    }
}
