using System.Drawing;

public static class Unscrambler{

    private static GifMaker gm = new GifMaker();
    private static int Counter = 0;

    public static Bitmap UnscrambleOriginal(PixelImage image, bool clearHistory = false){
        Bitmap original = new Bitmap(image.latest);
        foreach(var core in image.cores){
            if(core.history.Count > 0){
                original.SetPixel(core.x, core.y, Color.FromArgb(core.history[0]));
                if(clearHistory) core.ClearHistory();
            }
            else Console.WriteLine("WARNING: Trying to unscramble non-existant history");
        }
        return original;
    }

    public static Bitmap Unscramble(List<GridCore> cores, Bitmap legacy, int steps, bool generateAnimation = false)
    {
        List<Bitmap> bmps = new List<Bitmap>();
        Bitmap result = new Bitmap(legacy);

        foreach (var core in cores)
        {
            for (int i = steps; i > 0; i--)
            {
                core.Revert(1, true);
                result.SetPixel(core.x, core.y, (Color)core.color); // Cores colors are already set
                if (generateAnimation)
                {
                    bmps.Add(result);
                }
            }
        }

        if(generateAnimation){
            gm.Create(bmps, 000, 4, "unscramble" + ++Counter);
        }

        return result;
    }
}