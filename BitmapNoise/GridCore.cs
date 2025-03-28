using System.Drawing;
using System.IO.Pipelines;

public class GridCore
{

    public static int SimilarInstantExecute = 25;
    public const float SimilarDifferenceMultiplier = .1f;
    public readonly int x, y;
    public Color? color { get; private set; } = null;

    public HashSet<GridCore> neighbors { get; private set; } = new HashSet<GridCore>();
    private static Random rnd = new Random();
    public int? cachedColor { get; private set; } = null;
    public List<int> history { get; private set; } = new List<int>();

    public GridCore(int x, int y, Color color)
    {
        this.x = x;
        this.y = y;
        SetColor(color);
    }

    public GridCore(int x, int y)
    { //creates an empty core
        this.x = x;
        this.y = y;
    }

    public void AddNeighbor(GridCore neighbor)
    {
        if (!neighbors.Contains(neighbor))
        {
            neighbors.Add(neighbor);
            if (neighbor.color == null) Console.WriteLine("Neighbor was null!");
            //Console.WriteLine($"Added {neighbor.x} and {neighbor.y} as neighbor to {x}, {y} as neighbor {neighbors.Count}");
        }
    }

    public void ScrambleNeighbors(){
        if(neighbors.Count <= 1) {
            Console.WriteLine("WARNING: skipping scrambling as neighbor count was not high enough");
            return;
        }

        neighbors = neighbors.OrderBy(x => rnd.Next(neighbors.Count)).ToHashSet();
    }

    public void ClearNeighbors()
    {
        if (neighbors.Count > 0) neighbors.Clear();
        else Console.WriteLine($"Nothing to clean in core {x}, {y}");
    }

    public void ClearCache(){
        cachedColor = null;
    }

    public void SetColor(Color color, bool addToHistory = true)
    {
        this.color = color;
        if (cachedColor != null && color != Color.FromArgb((int)cachedColor))
        {
            Console.WriteLine("WARNING: Overrode cached core color!");
        }
        ClearCache();
        if (addToHistory) AddHistory(color.ToArgb());
    }

    public void AddHistory(int argb)
    {
        history.Add(argb);
    }

    public void ClearHistory()
    { //is this stupid..?
        int origin = history[0];
        history.Clear();
        AddHistory(origin);
    }

    public Color ColorPicker(ColorMode colorMode, bool set = true, bool reinterpretNull = false)
    {
        Color? result = null;
        IOrderedEnumerable<IGrouping<Color?, GridCore>> g = null;

        if (colorMode == ColorMode.Dominant || colorMode == ColorMode.Strength ||
        colorMode == ColorMode.Similar || colorMode == ColorMode.Minority || colorMode == ColorMode.Fade
        || colorMode == ColorMode.EveryOther)
        {
            g = neighbors.GroupBy(i => i.color)
                            .OrderByDescending(x => x.Count());
        }


        /*
        foreach(var grp in g){
            Console.WriteLine($"Groups in {x}, {y}:");
            Console.WriteLine("{0} {1}", grp.Key, grp.Count());
        }*/

        switch (colorMode)
        {
            case ColorMode.Dominant:
                //find neighbor which is most common
                result = g.First().Key;
                break;
            case ColorMode.Chance: //includes 1/3 chance to not change
                if (rnd.Next(3) == 0)
                {
                    result = color;
                }
                else
                {
                    result = neighbors.ElementAt(rnd.Next(neighbors.Count)).color;
                }
                break;
            case ColorMode.Random:
                //find random neighbor
                result = PickRandomColor(neighbors);
                break;
            case ColorMode.Strength:
                var list = g.ToList();
                result = RollRandom<IGrouping<Color?, GridCore>>.Roll(list, 2).Key;
                break;
            case ColorMode.Similar:
                result = AssimilateColor(SimilarInstantExecute, g); //is the color similar enough to assimilate immidietly?
                //if(result == g.First().Key) result = ColorPicker(ColorMode.Chance, false, true); //if it was, but the most similar color also was the dominant color, do chance instead
                if (result == null) Console.WriteLine("WARNING: Assimilation returned null");
                break;
            case ColorMode.Historic:
                result = PickHistoricColor();
                break;
            case ColorMode.Minority:
                result = g.Last().Key;
                break;
            case ColorMode.Fade:
                result = Transform((Color)g.First().Key, (Color)color, SimilarDifferenceMultiplier);
                break;
            case ColorMode.Unique: //Choose a random colormode for every pixel
                do result = ColorPicker((ColorMode)(rnd.Next(Enum.GetNames<ColorMode>().Count()) - 1), set, reinterpretNull);
                while (result == null);
                break;
            case ColorMode.EveryOther: //may stall if done too often
                if(x % 2 == 0 && y % 2 == 0) result = g.First().Key;
                break;
            case ColorMode.Monochrome:
                Color c = (Color)color;
                if(c.R >= 127 && c.B >= 127 && c.G >= 127) result = Color.White;
                else result = Color.Black;
                break;
            default:
                break;
        }


        if (result == null)
        {
            if (reinterpretNull) result = ColorPicker(ColorMode.Random, set, false);
            else Console.WriteLine("ERROR: Color was null");
        }

        if (set) SetColor((Color)result);
        else
        {
            Color resultColor = (Color)result;
            cachedColor ??= resultColor.ToArgb();
        }

        return result ?? Color.White;
    }

    private Color? PickHistoricColor() //may return the same color as the current color
    {
        if (history.Count <= 1)
        {
            //Console.WriteLine("ERROR: Trying to choose color from invalid history");
            return null;
        }
        int? result = null;
        int counter = 0;
        Color c = (Color)color;
        int tCol = c.ToArgb();
        do
        {
            result = history[rnd.Next(history.Count - 1)];
            counter++;
        }
        while (result == tCol && counter < history.Count);
        if(result == null) result = tCol;
        if(!history.Contains((int)result)) Console.WriteLine("Historic: ERROR: new color");
        return Color.FromArgb((int)result);
    }

    private Color PickRandomColor(HashSet<GridCore> neighbors)
    {
        var distinct = neighbors.Distinct().ToList();
        Color color = (Color)distinct[rnd.Next(distinct.Count)].color;
        return color;
    }

    private Color? AssimilateColor(int margin, IOrderedEnumerable<IGrouping<Color?, GridCore>> group)
    {
        if (margin > 255 || margin < 0)
        {
            Console.WriteLine("ERROR: Tried to assimilate invalid margin!");
            return null;
        }

        if (margin == 255 || margin == 0)
        {
            Console.WriteLine("WARNING: Inappropriate margin");
        }

        if (group == null)
        {
            Console.WriteLine("ERROR: Group was null");
            return null;
        }


        Color? result = null;
        Color? mostSimilar = null;
        foreach (var n in group)
        {
            Color curr = (Color)n.Key;
            Color tCol = (Color)color;
            if (IsSimilar(curr, tCol, margin))
            {
                result = curr;
                break;
            }

            if (mostSimilar == null) mostSimilar = group.First().Key;
            else if (CompareColor(curr, (Color)mostSimilar, tCol)) mostSimilar = curr;
        }

        return result ?? mostSimilar;
    }

    public static bool IsSimilar(Color curr, Color reference, int margin){
        return curr.A <= (reference.A + margin) && curr.A >= (reference.A - margin) && curr.R <= (reference.R + margin) && curr.R >= (reference.R - margin) &&
                curr.G <= (reference.G + margin) && curr.G >= (reference.G - margin) && curr.B <= (reference.B + margin) && curr.B >= (reference.B - margin);
    }

    private Color Transform(Color target, Color curr, float multiplier) // a multiplier between 0 and 1 is appropriate
    {
        int a = CalculateApproachingColor(target.A, curr.A, multiplier);
        int r = CalculateApproachingColor(target.R, curr.R, multiplier);
        int g = CalculateApproachingColor(target.G, curr.G, multiplier);
        int b = CalculateApproachingColor(target.B, curr.B, multiplier);
        //Console.WriteLine($"{color} became {Color.FromArgb(a, r, g, b)}");

        return Color.FromArgb(a, r, g, b);
    }

    public static int CalculateApproachingColor(int target, int curr, float multiplier)
    {
        int value = (int)((target - curr) * multiplier) + curr;
        //Console.WriteLine($"Result for {target} and {curr} was {value}");
        return (value >= 0 && value <= 255) ? value : 255;
    }

    public static bool CompareColor(Color a, Color b, Color reference)
    {
        int moreSimilar = 0;
        if (IsAMoreSimilar(a.A, b.A, reference.A)) moreSimilar++;
        if (IsAMoreSimilar(a.R, a.R, reference.R)) moreSimilar++;
        if (IsAMoreSimilar(a.G, b.G, reference.G)) moreSimilar++;
        if (IsAMoreSimilar(a.B, b.B, reference.B)) moreSimilar++;
        return moreSimilar >= 2; // it's more similar if 2 out of 4 aspects are closer (could be 3, but most likely all colors are alpha 255)
    }

    public static bool IsAMoreSimilar(int a, int b, int reference)
    {
        int first = Math.Abs(a - reference);
        int second = Math.Abs(b - reference);
        return first < second;
    }

    public Color Revert(int steps, bool clearFromHistory = false)
    {
        if (steps < 0 || steps > history.Count) steps = history.Count;

        Color result = Color.FromArgb(history[history.Count - steps]);
        SetColor(result, false);
        if (clearFromHistory) history.RemoveRange(history.Count - steps, steps);
        return result;
    }

    public void Convert32Colorto16()
    {
        throw new NotImplementedException();
        //sets alpha and B to 0 for some reason...
        Color color = (Color)this.color;
        byte a, r, g, b;
        r = (byte)(Convert.ToByte(color.R) >> 3);
        g = (byte)(Convert.ToByte(color.G) >> 3);
        b = (byte)(Convert.ToByte(color.B) >> 3);
        a = (byte)(Convert.ToByte(color.A) >> 7);
        a <<= 15;
        b <<= 10;
        g <<= 5;
        Color color16 = Color.FromArgb(a, r, g, b);
        SetColor(color16);
    }
}

public enum ColorMode
{
    Dominant, Chance, Random, Strength, Similar, Historic, Minority, Fade, Unique, EveryOther, Monochrome
}