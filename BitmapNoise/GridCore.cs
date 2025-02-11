using System.Drawing;

public class GridCore
{

    public readonly int x, y;
    public Color? color { get; private set; } = null;

    public HashSet<GridCore> neighbors { get; private set; } = new HashSet<GridCore>();
    private static Random rnd = new Random();

    public GridCore(int x, int y, Color color)
    {
        this.x = x;
        this.y = y;
        SetColor(color);
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

    public void ClearNeighbors(){
        if(neighbors.Count > 0) neighbors.Clear();
        else Console.WriteLine($"Nothing to clean in core {x}, {y}");
    }

    public void SetColor(Color color)
    {
        this.color = color;
    }

    public Color ColorPicker(ColorMode colorMode, bool set = true)
    {
        Color? result = null;

        switch (colorMode)
        {
            case ColorMode.Dominant:
                //find neighbor which is most common
                result = SortDominant();
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
            default:
                break;
        }


        if (result == null) Console.WriteLine("ERROR: Color was null");
        else if (set) SetColor((Color)result);

        return result ?? Color.White;
    }

    private Color? SortDominant(){
        var dic = new Dictionary<Color, int>();
        foreach(var n in neighbors){
            Color curr = (Color)n.color;
            if(!dic.ContainsKey(curr)){
                dic.Add(curr, 1);
            }
            else{
                dic[curr]++;
            }
        }

        Color? result = null;
        foreach(var pair in dic){
            if(result == null || pair.Value > dic[(Color)result]){
                result = pair.Key;
            }
        }

        return result;
    }

    private Color PickRandomColor(HashSet<GridCore> neighbors)
    {
        var distinct = neighbors.Distinct().ToList();
        Color color = (Color)distinct[rnd.Next(distinct.Count)].color;
        return color;
    }
}