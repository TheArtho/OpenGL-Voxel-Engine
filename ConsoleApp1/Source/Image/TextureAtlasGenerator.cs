using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

public class TextureAtlasGenerator
{
    public int Padding { get; set; }
    public int TileSize { get; set; }

    public TextureAtlasGenerator(int padding = 2, int tileSize = 16)
    {
        Padding = padding;
        TileSize = tileSize;
    }

    public Image<Rgba32> GeneratePaddedAtlas(Image<Rgba32> originalAtlas)
    {
        int tilesPerRow = originalAtlas.Width / TileSize;
        int tilesPerColumn = originalAtlas.Height / TileSize;

        // Calculating new dimensions
        int newWidth = tilesPerRow * (TileSize + 2 * Padding);
        int newHeight = tilesPerColumn * (TileSize + 2 * Padding);

        var newAtlas = new Image<Rgba32>(newWidth, newHeight);

        for (int y = 0; y < tilesPerColumn; y++)
        {
            for (int x = 0; x < tilesPerRow; x++)
            {
                // Source rectangle for the current tile
                var sourceRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);

                using (var clonedTile = originalAtlas.Clone(img => img.Crop(sourceRect)) as Image<Rgba32>)
                {
                    {
                        // Create an extended version of the tile with the correct padding size
                        var extendedTile = new Image<Rgba32>(TileSize + 2 * Padding, TileSize + 2 * Padding);
                        // Fill the extended tile with the edge pixels
                        ExtendEdges(clonedTile, extendedTile);

                        // Draw the extended tile into the new atlas
                        newAtlas.Mutate(ctx => ctx.DrawImage(
                            extendedTile,
                            new Point(x * (TileSize + 2 * Padding), y * (TileSize + 2 * Padding)),
                            1f));
                    }
                }
            }
        }

        return newAtlas;
    }


    private void ExtendEdges(Image<Rgba32> tile, Image<Rgba32> extendedTile)
    {
        // Fill the center of extendedTile with the original tile
        tile.Mutate(ctx => ctx.Resize(extendedTile.Width - 2 * Padding, extendedTile.Height - 2 * Padding));
        extendedTile.Mutate(ctx => ctx.DrawImage(tile, new Point(Padding, Padding), 1f));

        for (int p = 1; p <= Padding; p++)
        {
            // Extend top and bottom rows
            for (int x = Padding; x <= tile.Width + 1; x++)
            {
                // Top row
                extendedTile[x, p - 1] = extendedTile[x, Padding];
                // Bottom row
                extendedTile[x, TileSize + p + 1] = extendedTile[x, TileSize + Padding - 1];
            }

            // Extend left and right columns
            for (int y = Padding; y <= tile.Height + 1; y++)
            {
                // Left column
                extendedTile[p - 1, y] = extendedTile[Padding, y];
                // Right column
                extendedTile[TileSize + p + 1, y] = extendedTile[TileSize + Padding - 1, y];
            }
        }

        // Extend corners
        // Top-left corner
        extendedTile[Padding - 1, Padding - 1] = extendedTile[Padding, Padding];
        extendedTile[Padding - 2, Padding - 2] = extendedTile[Padding, Padding];
        extendedTile[Padding - 1, Padding - 2] = extendedTile[Padding, Padding];
        extendedTile[Padding - 2, Padding - 1] = extendedTile[Padding, Padding];
        // Top-right corner
        extendedTile[TileSize + Padding, Padding - 1] = extendedTile[TileSize + Padding - 1, Padding];
        extendedTile[TileSize + Padding, Padding - 2] = extendedTile[TileSize + Padding - 1, Padding];
        extendedTile[TileSize + Padding + 1, Padding - 2] = extendedTile[TileSize + Padding - 1, Padding];
        extendedTile[TileSize + Padding + 1, Padding - 1] = extendedTile[TileSize + Padding - 1, Padding];
        // Bottom-left corner
        extendedTile[Padding - 1, TileSize + Padding] = extendedTile[Padding, TileSize + Padding - 1];
        extendedTile[Padding - 2, TileSize + Padding] = extendedTile[Padding, TileSize + Padding - 1];
        extendedTile[Padding - 1, TileSize + Padding + 1] = extendedTile[Padding, TileSize + Padding - 1];
        extendedTile[Padding - 2, TileSize + Padding + 1] = extendedTile[Padding, TileSize + Padding - 1];
        
        // Bottom-right corner
        extendedTile[TileSize + Padding, TileSize + Padding] = extendedTile[TileSize + Padding - 1, TileSize + Padding - 1];
        extendedTile[TileSize + Padding + 1, TileSize + Padding] = extendedTile[TileSize + Padding - 1, TileSize + Padding - 1];
        extendedTile[TileSize + Padding, TileSize + Padding + 1] = extendedTile[TileSize + Padding - 1, TileSize + Padding - 1];
        extendedTile[TileSize + Padding + 1, TileSize + Padding + 1] = extendedTile[TileSize + Padding - 1, TileSize + Padding - 1];
    }

}
