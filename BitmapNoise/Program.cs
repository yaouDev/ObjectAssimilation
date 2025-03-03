using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Linq;

/* Bitmap logic
    [-1, -1][0, -1][1, -1]
    [-1, 0] [0, 0] [1, 0]
    [-1, 1] [0, 1] [1, 1]
*/

/// <summary>
/// TODO:
/// - reiterating cache should combine existing animations with the new animation
/// - 
/// </summary>

//NOT PUSHED!
//emergency break iterates once more ..?
//current clearhistory in gridcore *might* set both [0] and [1] to original

class Program
{

    public PixelImage curr;

    public static void Main(string[] args)
    {
        Program program = new Program();
        program.Run();
    }

    public void Run()
    {
        // var info = CreateNewConsole();
        while (Prompter());

    }

    private PixelImage CodePrompt()
    {
        BitmapInfo info = new BitmapInfo();
        info.debug = true;
        info.generateAnimation = true;
        info.saveIterations = false;

        PixelImage image = new PixelImage(info);

        //TEST CASE
        // image.GenerateBitmap(2500, 2500, image.GetLimitedColors(4), false); 
        //curr implementation, range 4: 2 seconds per generation, 26ish cores (W/ scramble 1.43), 7ish random, 4ish chance, 10 ish dominant, 40ish strength, similar 19ish, 15ish fade, minority 26ish

        //image.GenerateBitmap(500, 500, [Color.RoyalBlue, Color.Beige, Color.MediumSeaGreen], true); //islands
        //image.GenerateBitmap(250, 250, image.GetLimitedColors(8), true);
        // image.GenerateBitmap(100, 100, image.GetLimitedColors(5, true), false);
        // image.GenerateBitmap(100, 100, image.GetLimitedColors(6), false);
        image.GenerateBitmap(250, 250, [Color.FromArgb(0, 0, 0, 0), Color.Red, Color.Blue, Color.Green], false); //ARGB
        // image.GenerateBitmap(100, 100, [Color.Cyan, Color.Magenta, Color.Yellow, Color.Black], false); //CMYK
        // image.GenerateBitmap(100, 100, [Color.Black, Color.White], false);
        //image.GenerateBitmap(750, 750, image.GetLimitedColors(6), false);
        //image.Load("bullfinch.jpg");
        //Load("treesparrow.jpg");
        //image.Load("sword.jpg");
        //image.Load("commonswift.jpg");
        // image.Load("mountain_bluebird.jpg");
        // image.GenerateBitmap(250, 250, image.LoadColorPool("sword.jpg", int.MaxValue, true).ToArray(), false);
        // image.GenerateBitmap(250, 250, image.LoadColorPool("commonswift.jpg", int.MaxValue, true).ToArray(), false);
        //GenerateBitmap(500, 500, LoadColorPool("bullfinch.jpg", 12, true).ToArray(), true);

        int range = 2;
        image.AddIterations(250);
        // image.StartIteration(AssimilationAlgorithm.Chain, range, false, false);
        image.StartIteration(AssimilationAlgorithm.Random, range, false, false);
        // image.StartIteration(AssimilationAlgorithm.None, range, false, false, ColorMode.Fade);
        // image.StartIteration(AssimilationAlgorithm.None, range, false, false, ColorMode.Unique);
        //image.Log();

        return image;
    }

    private bool Prompter()
    {
        bool run = true;
        while (run)
        {
            if(curr != null && curr.emergencyBreak) curr.emergencyBreak = false;
            Console.WriteLine("---OBJECT ASSIMILATION v0.1---");
            Console.WriteLine("What would you like to do?" + $" --- Current cache: {(curr != null ? curr.uID : 0)}");
            Console.WriteLine("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}",
                                "1. Create new image",
                                "2. Load image from log",
                                "3. Test",
                                "4. Iterate cache",
                                "5. Clean edges of cache",
                                "6. Log cache",
                                "7. Turn cache into 16 bit colors",
                                "8. Revert cache x steps",
                                "9. Set cache to original",
                                "A. Print cache",
                                "0. Quit");
            Console.Write(">");

            switch (Read())
            {
                case "0":
                    run = false;
                    break;
                case "1":
                    curr = CreateNewConsole();
                    break;
                case "2":
                    curr = LoadFromLog();
                    break;
                case "3":
                    curr = CodePrompt();
                    break;
                case "4": // iterate bitmap
                    if (CheckCache())
                    {
                        //AssimilationAlgorithm alg = AlgorithmChoice();
                        AssimilationAlgorithm alg = (AssimilationAlgorithm)ChooseEnum<AssimilationAlgorithm>();

                        Console.WriteLine($"Algorithm: {alg}");

                        bool excludeNoise = false;
                        ColorMode master = ColorMode.Random;
                        if(alg == AssimilationAlgorithm.Random){
                            Console.Write("Exclude noise-based color modes? (y/n) >");
                            excludeNoise = Read() == "y";
                        }  
                        
                        if(alg == AssimilationAlgorithm.None){
                            master = (ColorMode)ChooseEnum<ColorMode>();
                        }

                        Console.Write("Enter neighbor range >");
                        int radius = int.Parse(Read());

                        Console.Write("Enter number of iterations >");
                        int iter = int.Parse(Read());

                        Console.Write("Set new info? (y/n) >");
                        bool newInfo = Read() == "y";
                        if(newInfo) curr.SetInfo(InfoPrompt());

                        curr.AddIterations(iter);
                        curr.StartIteration(alg, radius, false, excludeNoise, master);
                    }
                    break;
                case "5":
                    if (CheckCache())
                    {
                        Console.Write("Enter times to clean >");
                        int timesToClean = int.Parse(Read());
                        Console.Write("Enter radius to clean >");
                        int cleanRadius = int.Parse(Read());
                        curr.Clean(cleanRadius, timesToClean, true, true);
                    }
                    break;
                case "6":
                    if (CheckCache())
                    {
                        if (curr.cores == null || curr.cores.Count == 0)
                        {
                            curr.InitalizeCores(curr.neighborRange);
                        }
                        curr.Log();
                        Console.WriteLine("Cache was logged!");
                    }
                    break;
                case "7": // Convert to 16 bit color
                    if(CheckCache()){
                        curr.ConvertCoresTo16bit();
                    }
                    break;
                case "8": // revert x steps - add console info!
                    if (CheckCache())
                    {
                        Console.Write("Steps to revert >");
                        int steps = int.Parse(Read());
                        curr.Revert(steps, true);
                    }
                    break;
                case "9": //original
                    if (CheckCache())
                    {
                        curr.PrintOriginal(true);
                    }
                    break;
                case "a": //print
                    if(CheckCache()){
                        curr.SaveLatest();
                    }
                    break;
                default:
                    Console.WriteLine("Try again");
                    break;
            }
        }
        return run;
    }

    private TEnum ChooseEnum<TEnum>(){
        int counter = 0;
        Console.WriteLine($"Choose {typeof(TEnum)}");
        foreach(var n in Enum.GetNames(typeof(TEnum))){

            Console.WriteLine($"{++counter}. {n}");
        }
        Console.Write(">");
        int choice = int.Parse(Read());
        return (TEnum)Enum.Parse(typeof(TEnum), (choice - 1).ToString());
    }

    private bool CheckCache()
    {
        if (curr != null)
        {
            return true;
        }
        else
        {
            Console.WriteLine("ERROR: Nothing cached!");
            return false;
        }
    }

    private PixelImage LoadFromLog()
    {
        Console.Write("Enter log ID >");
        string path = Read();

        var info = Logger.BuildFromLog(path);

        PixelImage image = new PixelImage(info);

        image.GenerateFromLog(info, true, "rebuiltLastIter_");

        Console.WriteLine("Rebuilt cores from log!");
        return image;
    }

    private PixelImage CreateNewConsole()
    {
        PixelImage image;
        bool shouldLoad = false;

        while (true) //how this should be made...
        {
            Console.Write("Would you like to load or generate a file? (generate/load) >");
            string load = Read();
            if (load == "load" || load == "generate" || load == "l" || load == "g")
            {
                shouldLoad = load == "load" || load == "l"; //if the chosen method is to load, load.
                break;
            }
        }

        if (shouldLoad)
        {
            Console.WriteLine("Files can only be loaded from a Load-folder at root level");
            Console.Write("Enter the file name, including file ending >");
            string path = Read();
            image = new PixelImage(path);
        }
        else
        {
            Console.Write("Enter width (in pixels) >");
            int width = Int32.Parse(Read());
            Console.Write("Enter height (in pixels) >");
            int height = Int32.Parse(Read());

            image = new PixelImage(width, height);

            Console.Write("Load or choose colors? (load/choose) >");
            bool choose = Read() == "choose";

            Color[] colors;

            if (choose)
            {
                Console.Write("Enter amount of colors >");
                int amount = Int32.Parse(Read());
                colors = new Color[amount];
                Console.Write("Use random colors? (y/n) >");
                bool random = Read() == "y";

                for (int i = 0; i < amount;)
                {
                    Color? color = null;

                    if (random)
                    {
                        color = RollRandom<Color>.RandomColor();
                    }
                    else
                    {
                        Console.Write($"Color {i + 1} R >");
                        int r = Int32.Parse(Read());
                        Console.Write($"Color {i + 1} G >");
                        int g = Int32.Parse(Read());
                        Console.Write($"Color {i + 1} B >");
                        int b = Int32.Parse(Read());
                        Console.Write($"Color {i + 1} A >");
                        int a = Int32.Parse(Read());

                        try
                        {
                            color = Color.FromArgb(a, r, g, b);
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine(e);
                        }
                    }


                    if (color != null)
                    {
                        colors[i] = (Color)color;
                        Console.WriteLine("Added color: " + color.ToString());
                        i++;
                    }
                    else Console.WriteLine("Something went wrong, try again.");
                }
            }
            else
            {
                Console.Write("File name of image to load colors from >");
                colors = image.LoadColorPool(Read(), width * height).ToArray();
            }

            Console.Write("Use color priority? (y/n) >");
            bool priority = Read() == "y";

            image.GenerateBitmap(width, height, colors, priority);
        }

        return image;
    }

    private BitmapInfo InfoPrompt()
    {
        BitmapInfo info = new BitmapInfo();

        Console.Write("Launch in debug mode? (y/n) >");
        info.debug = Read() == "y";


        Console.Write("Save iterations? (y/n) >");
        info.saveIterations = Read() == "y";

        Console.Write("Generate animations from iterations? (SLOW IF MANY) (y/n) >");
        info.generateAnimation = Read() == "y";

        return info;
    }

    private string Read()
    {
        string str = "";
        try
        {
            str = Console.ReadLine();
        }
        catch (IOException e)
        {
            Console.WriteLine(e);
        }
        return str;
    }
}

public struct BitmapInfo
{
    public bool saveIterations;
    public bool generateAnimation;
    public bool debug;
}