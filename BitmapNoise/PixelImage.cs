using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

public class PixelImage
{
    private const int FrameWarningLimit = 100000;
    public static GifMaker gm = new GifMaker();
    public static Random rnd = new Random();
    private int iterationCounter;
    public const int ReshapingModulo = 100; //def 100?
    public bool emergencyBreak;
    public Bitmap latest { get; private set; }
    public List<GridCore> cores { get; private set; }
    public int uID { get; private set; }
    private int? rebuiltID = null;

    //---- parameters ----
    int iterations = 0;
    public int neighborRange;
    bool debug;
    bool generateAnimation;
    bool saveIterations;
    bool generated;

    //--------------------

    public PixelImage(Logger.LogInfo info)
    { // doesnt set neighbors
        cores = info.cores;
        latest = new Bitmap(info.width, info.height);
        GenerateUniqueID();
        rebuiltID = info.uniqueID;
    }

    public PixelImage(BitmapInfo info)
    {
        GenerateUniqueID();
        SetInfo(info);
    }

    public PixelImage(string path)
    {
        GenerateUniqueID();
        Load(path);
    }

    public PixelImage(int width, int height)
    {
        GenerateUniqueID();
        latest = new Bitmap(width, height);
    }

    public void SetInfo(BitmapInfo info)
    {
        debug = info.debug;
        generateAnimation = info.generateAnimation;
        saveIterations = info.saveIterations;
    }

    private void GenerateUniqueID()
    {
        if (uID != 0)
        {
            Console.WriteLine("WARNING: Trying to override unique ID");
            return;
        }
        else
        {
            uID = rnd.Next(9999); // make more complex..?
        }
    }

    public void Clean(int radius, int timesToClean, bool debug = false, bool save = false)
    {
        if (cores == null || cores.Count < 1 || radius != neighborRange) //reinitialize cores if radius changes
        {
            if (cores == null || cores.Count < 1) Console.WriteLine("WARNING: Cores weren't initialized - initializing...");
            neighborRange = radius;
            InitalizeCores(neighborRange);
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        if (!emergencyBreak)
        {
            Console.WriteLine($"Cleaning {timesToClean} time(s)...");

            do
            {
                if (timesToClean == 1) IterateBitmap(ColorMode.Dominant, false, "_lastCleaning", false, true); //saves the last cleaning - generate animation could be set to debug
                else IterateBitmap(ColorMode.Dominant, false, "", false, false);
                timesToClean--;
            }
            while (timesToClean > 0 && !emergencyBreak);
        }
        else
        {
            Console.WriteLine("WARNING: Image cleaning was skipped");
        }

        if (emergencyBreak) BuildLatest();

        Console.WriteLine($"Cleaning took {stopwatch.Elapsed}");

        Save(latest, "cleaned");
        stopwatch.Stop();
    }

    public List<Color> LoadColorPool(string path, int maxLimit = Int32.MaxValue, bool useInstantExecute = false)
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

                    if (useInstantExecute)
                    {
                        bool foundSimilar = false;
                        int counter = 0;
                        while(!foundSimilar && counter < pool.Count){
                            if(GridCore.IsSimilar(color, pool[counter], GridCore.SimilarInstantExecute)){
                                foundSimilar = true;
                            }
                            counter++;
                        }
                        if(!foundSimilar) pool.Add(color);
                    }
                    else pool.Add(color);
                }
            }
        }
        if(pool.Count == 0) Console.WriteLine("WARNING: Color pool is empty!");
        else{
            foreach(var c in pool){
                Console.WriteLine($"{c}");
            }
        }


        return pool;
    }

    public List<Bitmap> StartIteration(AssimilationAlgorithm alg, int radius, bool clean = true, bool excludeNoise = false, ColorMode master = ColorMode.Random)
    {
        Stopwatch stopWatch = new Stopwatch();
        TimeSpan totalTime = new TimeSpan();
        stopWatch.Start();
        ColorMode colorMode;
        bool isIterated = iterations > 0;
        int iterationsToBeMade = iterations + iterationCounter;
        List<Bitmap> bitmaps = new List<Bitmap>();
        if (generateAnimation) bitmaps.Add(latest);

        if (!isIterated) Console.WriteLine("WARNING: No iterations!");
        else if (radius != neighborRange)
        {
            neighborRange = radius;
            Console.WriteLine("Initializing cores...");
            InitalizeCores(neighborRange);
            if (debug)
            {
                Console.WriteLine("Initializing cores took " + (stopWatch.Elapsed - totalTime));
                totalTime = stopWatch.Elapsed;
            }
        }

        if (debug) Console.WriteLine(this.ToString()); //image info
        Console.WriteLine($"Starting {iterations} iterations --- {alg}");

        while (iterations > 0 && !emergencyBreak)
        {
            //EXPERIMENTAL!!!
            /*
            if(iterationCounter % 100 == 0){
                neighborRange *= 2;
                InitalizeCores(neighborRange);
            }*/

            switch (alg)
            {
                case AssimilationAlgorithm.Random:
                    colorMode = RandomColorMode(excludeNoise);
                    break;
                case AssimilationAlgorithm.Chain:
                    colorMode = ChainBasedColorMode(excludeNoise);
                    break;
                case AssimilationAlgorithm.Modulo:
                    colorMode = ModuloBasedColorMode();
                    break;
                case AssimilationAlgorithm.None:
                    colorMode = master;
                    break;
                default:
                    Console.WriteLine("ERROR: Algorithm choice failed");
                    throw new InvalidDataException();
            }

            if (generateAnimation)
            {
                Bitmap iter = IterateBitmap(colorMode, saveIterations, "", false, true);
                bitmaps.Add(iter);
            }
            else IterateBitmap(colorMode, saveIterations, "", false, false);

            if (debug) Console.WriteLine($"{uID}: Iteration {iterationCounter}/{iterationsToBeMade} took {stopWatch.Elapsed - totalTime} : {colorMode}");

            totalTime = stopWatch.Elapsed;
            iterations--;
        }

        if (debug) Console.WriteLine("Iterating took: " + stopWatch.Elapsed);

        if (isIterated)
        {
            if (clean)
            {
                int timesToClean = iterationCounter.ToString().Length - 1;
                if (timesToClean < 1) timesToClean = 1;

                Clean(neighborRange, timesToClean, debug);
            }
            else
            {
                BuildLatest();
            }

            Save(latest, "finalIter" + iterationCounter);
        }

        if (generateAnimation && (iterations == 0 || emergencyBreak))
        {
            gm.Create(bitmaps, uID, 4, "Iter" + iterationCounter);

            //Boomerang (:
            List<Bitmap> rBitmaps = [.. bitmaps];
            rBitmaps.Reverse();
            List<Bitmap> loop = [.. bitmaps.Concat(rBitmaps)];

            gm.Create(loop, uID, 4, "_boomerangIter" + iterationCounter, 0); // should loop forever with 0 but doesn't...
        }


        totalTime = stopWatch.Elapsed;
        Console.WriteLine("Total time: " + totalTime);
        stopWatch.Stop();

        if (emergencyBreak) iterations = 0;

        return bitmaps;
    }

    public void AddIterations(int addition)
    {
        iterations += addition;
    }

    private ColorMode ModuloBasedColorMode() //uses global iterationCounter
    {
        ColorMode colorMode = ColorMode.Chance; //works as a default
        if (iterationCounter != 0)
        {
            if (iterationCounter % ReshapingModulo == 0) colorMode = ColorMode.Dominant;
            else if (iterationCounter % (ReshapingModulo * 2.9f) == 0 && iterations != 1) colorMode = ColorMode.Historic;
            else if (iterationCounter % (ReshapingModulo * 1.53f) == 0) colorMode = ColorMode.Strength;
            else if (iterationCounter % (ReshapingModulo * 1.27f) == 0 && iterations != 1) colorMode = ColorMode.Minority;
            else if (iterationCounter % (ReshapingModulo * .22f) == 0) colorMode = ColorMode.Fade;
            else if (iterationCounter % 5 == 0 && iterations != 1) colorMode = ColorMode.Similar;
        }

        return colorMode;
    }

    private ColorMode ChainBasedColorMode(bool excludeNoise)
    {
        ColorMode colorMode = ColorMode.Similar; //works as a default
        if (iterationCounter != 0)
        {
            if (iterationCounter % 2 == 0) colorMode = ColorMode.Dominant;
            else if (iterationCounter % 3 == 0) colorMode = ColorMode.Fade;
            else if (iterationCounter % 5 == 0) colorMode = ColorMode.EveryOther;
            else if (iterationCounter % 7 == 0) colorMode = ColorMode.Minority;
            else if (iterationCounter % 23 == 0) colorMode = ColorMode.Unique;
            else if (!excludeNoise && iterationCounter % 29 == 0) colorMode = ColorMode.Strength;
            else if (!excludeNoise && iterationCounter % 53 == 0) colorMode = ColorMode.Chance;
        }

        return colorMode;
    }

    private ColorMode RandomColorMode(bool excludeNoise = false)
    {
        var count = Enum.GetNames(typeof(ColorMode));
        List<ColorMode> excludedModes = new List<ColorMode>();
        if (excludeNoise) excludedModes = new List<ColorMode> { ColorMode.Random, ColorMode.Chance,
                                                                 ColorMode.Strength, ColorMode.Historic, 
                                                                 ColorMode.Unique, ColorMode.EveryOther };
        ColorMode? choice = null;
        do
        {
            choice = (ColorMode)rnd.Next(count.Length);
        } // this can definitely be done without reiterating if the "wrong" mode is chosen...
        while (iterationCounter == 0 && (choice == ColorMode.Historic || choice == ColorMode.Minority) || excludedModes.Contains((ColorMode)choice));
        //if this is the first iteration, dont use legacy based colormodes. or if excludeNoise in enabled, dont use noise based colormodes


        return (ColorMode)choice;
    }

    private Bitmap IterateBitmap(ColorMode colorMode, bool save = false, string prepend = "", bool generateAnimation = false, bool buildLatest = true) //generateAnimation is very slow on large maps
    {
        Bitmap? result = (save || generateAnimation || buildLatest) ? new Bitmap(latest) : null;


        List<Bitmap> bmps = null;
        if (generateAnimation) bmps = new List<Bitmap>();

        Color? check = null;
        emergencyBreak = true; //turn off if other color is found
        /*foreach (var core in cores)
        {
            Color curr = core.ColorPicker(colorMode, false, true); //cache colors
            if (check == null) check = curr;
            else if (emergencyBreak && check != curr) emergencyBreak = false;
        }*/

        Parallel.ForEach(cores, core => {
            Color curr = core.ColorPicker(colorMode, false, true); //cache colors
            if (check == null) check = curr;
            else if (emergencyBreak && check != curr) emergencyBreak = false;
        });

        //if (debug) Console.WriteLine("Setting cached colors...");
        foreach (var core in cores)
        {
            if (core.cachedColor != null)
            {
                core.SetColor(Color.FromArgb((int)core.cachedColor));
                if (save || generateAnimation || buildLatest) result.SetPixel(core.x, core.y, (Color)core.color);
                if (generateAnimation) // very, very slow as to not store too much in memory
                {
                    Bitmap debugMap = new Bitmap(result);
                    debugMap.SetPixel(core.x, core.y, Color.Red);
                    bmps.Add(debugMap);
                }
            }
            else
            {
                Console.WriteLine($"Cached color was null in core {core.x}, {core.y}");
            }
        }

        if (save) Save(result, prepend + "iteration_" + ++iterationCounter);
        else iterationCounter++;

        if (generateAnimation)
        {
            bool cont;
            int frames = bmps.Count;
            if (frames > FrameWarningLimit)
            {
                Console.WriteLine($"WARNING: Creating a(n) {prepend} animation from {frames} frames will be very slow and take up a lot of memory.\nContinue? (y/n)");
                cont = Console.Read() == 'y';
            }
            else cont = true;

            if (cont)
            {
                Console.WriteLine($"Generating iteration animation from {frames} frames");
                gm.Create(bmps, uID, 4, prepend);
            }
        }
        if (emergencyBreak) Console.WriteLine($"WARNING: iterations stopped during iteration {iterationCounter + 1} as only one color remained");

        //Will spew errors if emergencybreak is uplled as colors as cached and never reset.

        if (result != null) latest = result; //result may be null, because we dont always want to cache a bitmap
        return result;
    }

    private Bitmap BuildLatest()
    { //Should not be used if iterating the Bitmap!
        Bitmap result = new Bitmap(latest);

        foreach (var core in cores)
        {
            result.SetPixel(core.x, core.y, (Color)core.color);
        }

        latest = result;

        return result;
    }

    public void InitalizeCores(int radius, bool clear = true, bool debug = false)
    {
        if (cores == null)
        {
            cores = new List<GridCore>();
        }

        int lWidth = latest.Width;
        int lHeight = latest.Height;

        if (clear && cores.Count > 0) cores.Clear();

        GridCore[,] grid = new GridCore[lWidth, lHeight];

        if (debug) Console.WriteLine("Cores to iterate: " + (lWidth * lHeight) * (radius * radius)); // ((radius * radius) + 1) ??? doesnt match frames

        for (int x = 0; x < lWidth; x++)
        {
            for (int y = 0; y < lHeight; y++)
            {
                grid[x, y] = new GridCore(x, y, latest.GetPixel(x, y));
            }
        }

        if (debug) Console.WriteLine("Generated core grid, setting neighbors...");

        Parallel.For(0, lWidth, x =>
        {
            Parallel.For(0, lHeight, y =>
            {
                SetCoreNeighbors(grid[x, y], grid, radius, lWidth, lHeight, clear);
            });
        });

        foreach (var core in grid)
        {
            core.ScrambleNeighbors(); // makes the neighbors unordered, adds radomization
            cores.Add(core);
        }
    }

    private void SetCoreNeighbors(GridCore core, GridCore[,] grid, int radius, int width, int height, bool clear = true)
    {
        //once a cores' neighbors are set, iteration on that core could begin...
        if (clear && core.neighbors.Count > 0) core.ClearNeighbors();

        List<GridCore> neighbors = new List<GridCore>();

        for (int i = core.x - radius; i <= (core.x + radius); i++)
        {
            for (int j = core.y - radius; j <= (core.y + radius); j++)
            {
                if (i >= 0 && j >= 0 && i < width && j < height && (i != core.x || j != core.y))
                {
                    core.AddNeighbor(grid[i, j]);
                }
            }
        }
    }

    public Bitmap GenerateBitmap(int width = 100, int height = 100, Color[]? colorPool = null, bool usePriority = false)
    {
        Stopwatch timer = new Stopwatch();
        timer.Start();

        Bitmap bitmap = new Bitmap(width, height);

        for (int x = 0; x < width; x++)
        {
            //insert parallelism?
            for (int y = 0; y < height; y++)
            {
                if (colorPool == null)
                {
                    bitmap.SetPixel(x, y, RollRandom<Color>.RandomColor());
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
                        bitmap.SetPixel(x, y, RollRandom<Color>.Roll(colorPool.ToList(), 2));
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

        Console.WriteLine($"Generating {width} x {height} bitmap took {timer.Elapsed}");
        return bitmap;
    }

    public Color[] GetLimitedColors(int numColors, bool similar = false)
    {
        Color[] colors = new Color[numColors];

        Color? main = null;
        if (similar) main = RollRandom<Color>.RandomColor();
        for (int i = 0; i < numColors; i++)
        {
            if (similar) colors[i] = RollRandom<Color>.SimilarColor((Color)main, GridCore.SimilarInstantExecute);
            else colors[i] = RollRandom<Color>.RandomColor();
        }

        return colors;
    }

    public void SaveLatest()
    {
        Save(latest, "print");
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
        string fileName = uID + "_" + prepend + time + (generated ? "_generated" : "_loaded") + "Image.png";
        return fileName;
    }

    public void Load(string path)
    {
        string file = "./Load/" + path;
        generated = false;
        latest = (Bitmap)Bitmap.FromFile(file);

        Console.WriteLine("Loaded " + path + " at " + latest.Width + " x " + latest.Height);
    }

    public void GenerateFromLog(Logger.LogInfo log, bool save = false, string nameAddition = "") //need access to height and width
    {
        Bitmap result = new Bitmap(log.width, log.height);
        foreach (var core in cores)
        {
            result.SetPixel(core.x, core.y, (Color)core.color);
        }
        latest = result;
        if (save) Save(latest, nameAddition + rebuiltID);
    }

    public void Log()
    {
        Console.WriteLine($"Logging {uID}...");
        Logger.Log(cores, uID);
    }

    public void PrintOriginal(bool setLatest = false)
    {
        Bitmap original = Unscrambler.UnscrambleOriginal(this, setLatest); //clears the history if latest is set.
        if (setLatest)
        {
            latest = original;
            //clear all history..?
        }
        Save(original, "original");
    }

    public void Revert(int steps, bool clearHistory = false)
    {
        Console.WriteLine($"Reverting {steps} steps...");
        foreach (var core in cores)
        {
            core.Revert(steps, clearHistory);
        }
        if (clearHistory) iterationCounter -= steps;
        BuildLatest();
    }

    public void ConvertCoresTo16bit()
    {
        foreach (var core in cores)
        {
            Color was = (Color)core.color;
            core.Convert32Colorto16();
            Console.WriteLine($"Turned {was} into {core.color}");
        }
        BuildLatest();
    }

    public override string ToString()
    {
        string result = $"---Image {uID}---\nGenerated: {generated}\nDebug: {debug}\nGenerate Animation: {generateAnimation}\nSave Iterations: {saveIterations}\nNeighbor Range: {neighborRange}\n";
        result += "---------------";

        return result;
    }
}

public enum AssimilationAlgorithm
{
    Random, Chain, Modulo, None
}