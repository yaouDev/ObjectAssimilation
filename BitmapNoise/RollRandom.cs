using System.Collections.ObjectModel;
using System.Drawing;

public static class RollRandom<T>{

    private static Random rnd = new Random();

    public static T Roll(List<T> array, int division) //division should be greater than 1 (1 = 100%)
    {
        //implement a timeout?

        if (array.Count == 0)
        {
            Console.WriteLine("ERROR: No objects!");
            throw new InvalidDataException();
        }

        T? result = default;
        bool done = false;

        int i = 0;
        while (!done)
        {
            if (i == array.Count)
            {
                i = 0;
            }
            else
            {
                int roll = rnd.Next(division);
                if (roll == 0)
                {
                    //result = array[i];
                    result = array.ElementAt(i);
                    done = true;
                }
                i++;
            }
        }

        if (result == null) Console.WriteLine("ERROR: Result was null in RollRandom!");
        return result;
    }

    public static Color RandomColor()
    {
        return Color.FromArgb(255, rnd.Next(256), rnd.Next(256), rnd.Next(256));
    }

    public static Color SimilarColor(Color color, int margin){
        return Color.FromArgb(ColorValueMargin(color.A, margin), ColorValueMargin(color.R, margin), ColorValueMargin(color.G, margin), ColorValueMargin(color.B, margin));
    }

    private static int ColorValueMargin(int value, int margin){
        int a = rnd.Next(value - margin, value + margin + 1);
        return a <= 255 && a >= 0 ? a : 255;
    }
}