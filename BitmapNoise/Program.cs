using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

/* Bitmap logic
    [-1, -1][0, -1][1, -1]
    [-1, 0] [0, 0] [1, 0]
    [-1, 1] [0, 1] [1, 1]
*/

//Fails to push as ignored is not properly ignored....

class Program
{

    private static Bitmap latest;
    private List<GridCore> cores;
    private int latestWidth;
    private int latestHeight;
    private static int IterationCounter;
    private Random rnd = new Random();
    private int currID = 0;
    private bool generated;
    private bool debug;
    private GifMaker gm = new GifMaker();

    //--- For console prompt
    private bool shouldLoad;
    private bool experimental;
    private int dominantModulo = 100;
    private bool emergencyBreak;
    //---

    public static void Main(string[] args)
    {
        Program program = new Program();
        program.Run();
    }

    public void Run()
    {
        // var info = ConsolePrompt();
        var info = CodePrompt();

        StartIteration(info.Item1, info.Item2, false);

        Console.WriteLine("FINISHED!");
        Console.ReadLine();
    }

    private Tuple<int, int> CodePrompt()
    {
        //GenerateBitmap(500, 500, [Color.RoyalBlue, Color.Beige, Color.MediumSeaGreen], true); //islands
        //GenerateBitmap(2500, 2500, GetLimitedColors(4), false); //curr implementation, range 4: 55...? seconds per "chance"
        GenerateBitmap(500, 500, GetLimitedColors(12), true);
        //GenerateBitmap(500, 500, GetLimitedColors(6), false);
        //GenerateBitmap(100, 100, [Color.Black, Color.White], false);
        //Load("bullfinch.jpg");
        //Load("treesparrow.jpg");
        //GenerateBitmap(500, 500, LoadColorPool("sword.jpg").ToArray(), false);

        debug = true;

        int neighborRange = 2;
        int iterations = 500;
        return new Tuple<int, int>(iterations, neighborRange);
    }

    private Tuple<int, int> ConsolePrompt()
    {
        Console.Write("Launch in debug mode? (y/n) >");
        debug = Read() == "y";

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
            Load(path);
        }
        else
        {
            Console.Write("Enter width (in pixels) >");
            int width = Int32.Parse(Read());
            Console.Write("Enter height (in pixels) >");
            int height = Int32.Parse(Read());

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
                        color = RandomColor();
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
                        Console.Write("Added color: " + color.ToString());
                        i++;
                    }
                    else Console.WriteLine("Something went wrong, try again.");
                }
            }
            else
            {
                Console.Write("File name of image to load colors from >");
                colors = LoadColorPool(Read(), width * height).ToArray();
            }

            Console.Write("Use color priority? (y/n) >");
            bool priority = Read() == "y";

            GenerateBitmap(width, height, colors, priority);
        }

        Console.Write("Number of iterations >");
        int iterations = Int32.Parse(Read());
        Console.Write("Neighbor Range (radius in pixels) >");
        int range = Int32.Parse(Read());

        Console.Write("Use experimental variations? (y/n) >");
        experimental = Read() == "y";
        if (experimental)
        {
            Console.Write("Enter n-th time an experimental should be generated >");
            dominantModulo = Int32.Parse(Read());
        }

        return new Tuple<int, int>(iterations, range);
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

    private List<Bitmap> StartIteration(int iterations, int neighborRange, bool saveIterations = false)
    {
        Console.WriteLine("Starting iterations...");
        Stopwatch stopWatch = new Stopwatch();
        TimeSpan totalTime = new TimeSpan();
        stopWatch.Start();
        ColorMode colorMode;
        bool isIterated = iterations > 0;
        List<Bitmap> bitmaps = new List<Bitmap>();


        if (!isIterated) Console.WriteLine("WARNING: No iterations!");
        else
        {
            InitalizeCores(neighborRange);
            if (debug) Console.WriteLine("Initializing cores took " + (stopWatch.Elapsed - totalTime));
            totalTime = stopWatch.Elapsed;
        }

        while (iterations > 0 && !emergencyBreak)
        {
            //--- Additional reshaping
            if (IterationCounter % 3 == 0 && iterations != 1) colorMode = ColorMode.Random;
            else if (experimental && IterationCounter % dominantModulo == 0) colorMode = ColorMode.Dominant; //does it work?
            else colorMode = ColorMode.Chance; //this will work as a default color mode
            //Console.WriteLine("Color mode is: " + colorMode);
            //---

            Bitmap iter = IterateBitmap(colorMode, saveIterations);
            bitmaps.Add(iter);
            if (debug) Console.WriteLine("Iteration " + IterationCounter + " took " + (stopWatch.Elapsed - totalTime));
            totalTime = stopWatch.Elapsed;

            iterations--;
        }

        if (debug) Console.WriteLine("Generating animation...");
        gm.Create(bitmaps, currID, true);

        if (isIterated)
        {
            if (debug) Console.WriteLine("Cleaning up image...");
            Save(latest, "pre-cleanup");
            if (neighborRange > 1)
            {
                InitalizeCores(1);
            }
            IterateBitmap(ColorMode.Dominant, false, "_cleaning", true);

            Console.WriteLine("Clean up took: " + (stopWatch.Elapsed - totalTime));
        }
        else
        {
            Console.WriteLine("Image was not cleaned as it was not iterated");
        }

        totalTime = stopWatch.Elapsed;
        Console.WriteLine("Total time: " + totalTime);
        stopWatch.Stop();

        Save(latest, "final_iter" + (IterationCounter - 1));

        latestHeight = latest.Height;
        latestWidth = latest.Width;

        return bitmaps;
    }

    private void Load(string path)
    {
        string file = "./Load/" + path;
        currID = rnd.Next(9999);
        generated = false;
        latest = (Bitmap)Bitmap.FromFile(file);
        latestHeight = latest.Height;
        latestWidth = latest.Width;

        Console.WriteLine("Loaded " + path + " at " + latestWidth + " x " + latestHeight);
    }

    private List<Color> LoadColorPool(string path, int maxLimit = Int32.MaxValue)
    {
        string file = "./Load/" + path;
        Bitmap poolSrc = (Bitmap)Bitmap.FromFile(file);
        List<Color> pool = new List<Color>();
        for (int x = 0; x < poolSrc.Width; x++)
        {
            for (int y = 0; y < poolSrc.Height; y++)
            {
                Color color = poolSrc.GetPixel(x, y);
                if (!pool.Contains(color))
                {
                    if (pool.Count > maxLimit) break;
                    pool.Add(color);
                    if (debug)
                    {
                        Console.WriteLine($"Color {pool.Count}: {color}");
                    }
                }
            }
        }

        return pool;
    }

    private void InitalizeCores(int radius, bool clear = true)
    {
        if (cores == null)
        {
            cores = new List<GridCore>();
        }

        if (clear && cores.Count > 0) cores.Clear();

        GridCore[,] grid = new GridCore[latestWidth, latestHeight];

        if (debug) Console.WriteLine("Cores to iterate: " + (latestWidth * latestHeight) * (radius * radius)); // ((radius * radius) + 1) ??? doesnt match frames

        for (int x = 0; x < latestWidth; x++)
        {
            for (int y = 0; y < latestHeight; y++)
            {
                grid[x, y] = new GridCore(x, y, latest.GetPixel(x, y));
            }
        }

        if (debug) Console.WriteLine("Generated core grid, setting neighbors...");

        Parallel.For(0, latestWidth, x =>
        {
            Parallel.For(0, latestHeight, y =>
            {
                SetCoreNeighbors(grid[x, y], grid, radius, clear);
            });
        });

        foreach (var core in grid)
        {
            cores.Add(core);
        }
    }

    private void SetCoreNeighbors(GridCore core, GridCore[,] grid, int radius, bool clear = true)
    {
        if (clear && core.neighbors.Count > 0) core.ClearNeighbors();

        for (int i = core.x - radius; i <= (core.x + radius); i++)
        {
            for (int j = core.y - radius; j <= (core.y + radius); j++)
            {
                if (i >= 0 && j >= 0 && i < latestWidth && j < latestHeight && (i != core.x || j != core.y))
                {
                    core.AddNeighbor(grid[i, j]);
                }
            }
        }
    }

    private Bitmap IterateBitmap(ColorMode colorMode, bool save = false, string prepend = "", bool clean = false, bool generateAnimation = false) //generateAnimation is very slow on large maps
    {
        Bitmap result = new Bitmap(latest);
        List<Bitmap> bmps = new List<Bitmap>();

        Color? check = null;
        emergencyBreak = true; //turn off if other color is found
        foreach (var core in cores)
        {
            Color curr = core.ColorPicker(colorMode, false);
            if(check == null) check = curr;
            else if (emergencyBreak && check != curr) emergencyBreak = false;
        }

        if (!emergencyBreak)
        {
            if (debug && clean) Console.WriteLine("Setting cached colors...");
            foreach (var core in cores)
            {
                if (core.cachedColor != null)
                {
                    core.SetColor(Color.FromArgb((int)core.cachedColor));
                    result.SetPixel(core.x, core.y, (Color)core.color);
                    if (generateAnimation) // very, very slow as to not store too much in memory
                    {
                        bmps.Add(result);
                    }
                }
                else
                {
                    Console.WriteLine($"Cached color was null in core {core.x}, {core.y}");
                }
            }

            if (generateAnimation)
            {
                Console.WriteLine($"Generating iteration/clean up animation from {bmps.Count} frames");
                gm.Create(bmps, currID, true, prepend);
            }

            IterationCounter++;
            if (save) Save(result, prepend + "iteration_" + IterationCounter);
        }
        else Console.WriteLine($"WARNING: iterations stopped during iteration {IterationCounter + 1} as only one color remained");

        latest = result;
        return result;
    }

    private Bitmap GenerateBitmap(int width = 100, int height = 100, Color[]? colorPool = null, bool usePriority = false)
    {
        Bitmap bitmap = new Bitmap(width, height);
        currID = rnd.Next(9999);

        for (int x = 0; x < width; x++)
        {
            //insert parallelism?
            for (int y = 0; y < height; y++)
            {
                if (colorPool == null)
                {
                    bitmap.SetPixel(x, y, RandomColor());
                }
                else
                {
                    if (usePriority)
                    {
                        if (colorPool.Length == 0)
                        {
                            Console.WriteLine("ERROR: Cannot use priority on empty color pool!");
                            return null;
                        }
                        bitmap.SetPixel(x, y, IndexPriority(colorPool, 2));
                    }
                    else
                    {
                        //choose from colors in color pool
                        bitmap.SetPixel(x, y, colorPool[rnd.Next(colorPool.Length)]);
                    }
                }


                //Console.WriteLine(x + ", " + y + " set to " + bitmap.GetPixel(x, y).Name);
            }
        }

        generated = true;
        Save(bitmap, "initial");
        latest = bitmap;
        latestHeight = latest.Height;
        latestWidth = latest.Width;

        return bitmap;
    }

    private Color RandomColor()
    {
        return Color.FromArgb(255, rnd.Next(256), rnd.Next(256), rnd.Next(256));
    }

    private Color[] GetLimitedColors(int numColors)
    {
        Color[] colors = new Color[numColors];

        for (int i = 0; i < numColors; i++)
        {
            colors[i] = RandomColor();
        }

        return colors;
    }

    private void Save(Bitmap bmp, string prepend = "")
    {
        string fileName = GenerateFileName(prepend);
        bmp.Save(fileName, ImageFormat.Png);
        Console.WriteLine("Saved: " + fileName);
    }

    private string GenerateFileName(string prepend = "")
    {
        if (prepend != "") prepend += "_";
        string time = DateTime.Now.ToShortDateString();
        string fileName = currID + "_" + prepend + time + (generated ? "_generated" : "_loaded") + "Image.png";
        return fileName;
    }

    private Color IndexPriority(Color[] colors, int division) //division should be greater than 1 (1 = 100%)
    {
        if (colors.Length == 0)
        {
            Console.WriteLine("ERROR: No colors!");
            throw new InvalidDataException();
        }

        Color? result = null;
        bool done = false;

        int i = 0;
        while (!done)
        {
            if (i == colors.Length)
            {
                i = 0;
            }
            else
            {
                int roll = rnd.Next(division);
                if (roll == 0)
                {
                    result = colors[i];
                    done = true;
                }
                i++;
            }
        }

        if (result == null) Console.WriteLine("ERROR: Color was null in IndexPriority!");
        Color newResult = result ?? Color.White;
        return newResult;
    }
}

public enum ColorMode
{
    Dominant, Chance, Random
}