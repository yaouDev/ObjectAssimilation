using System.Drawing;

public static class EffectHandler
{
    public static void HandleEffect(Effect effect, List<GridCore> cores)
    {
        switch (effect)
        {
            case Effect.NoisedBlackAndWhite:
                NoisedBlackAndWhite(cores);
                break;
            default:
                Console.WriteLine("EffectHandler failed.");
                break;
        }
    }

    private static void NoisedBlackAndWhite(List<GridCore> cores)
    { // make it have shades?
        Parallel.ForEach(cores, core => {
            core.ColorPicker(ColorMode.Monochrome, false, false);
        });

        cores.ForEach(core => {
            if (core.cachedColor != null)
            {
                Color cache = Color.FromArgb((int)core.cachedColor);
                core.ClearCache();
                Color other = cache.R > 127 ? Color.Black : Color.White;
                core.SetColor(RollRandom<Color>.Roll([cache, other], 2));
            }
        });
    }
}