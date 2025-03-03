using System.Drawing;
using System.Text;

//NOTE: The logger does not rebuild the neighbors!

public static class Logger
{
    public static void Log(List<GridCore> cores, int currID)
    {
        // : separates core info
        // & separates cores
        // ! signifies dimensions


        if(cores.Count < 1){
            Console.WriteLine("LOGGER: Cannot log nothing.");
            return;
        }

        string path = "./Logs/";

        using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, $"{currID}_log.txt")))
        {
            int lastX = 0;
            int lastY = 0;

            StringBuilder sb = new StringBuilder();

            foreach (var core in cores)
            {
                string coreHistory = "";
                foreach (int entry in core.history)
                {
                    coreHistory += ":" + entry;
                }
                //outputFile.WriteLine($"{core.x}:{core.y}{coreHistory}");
                sb.Append($"&{core.x}:{core.y}{coreHistory}");
                if (core.x > lastX) lastX = core.x + 1; //these require + 1 as it works like an array
                if (core.y > lastY) lastY = core.y + 1;
            }
            //outputFile.WriteLine($"!{lastX}!{lastY}");
            sb.Append($"!{lastX}!{lastY}");
            outputFile.WriteLine(sb.ToString());
        }
    }

    public static LogInfo BuildFromLog(string logFile)
    {
        List<GridCore> newCores = new List<GridCore>();
        int width = 0;
        int height = 0;

        try
        {
            using (StreamReader sr = new StreamReader("./Logs/" + logFile + "_log.txt"))
            {
                string line = sr.ReadLine();
                /*
                while (line != null)
                {
                    if (line.First() != '!') newCores.Add(InterpretCore(line));
                    else
                    {
                        string[] coords = line.Split('!', StringSplitOptions.RemoveEmptyEntries);
                        width = int.Parse(coords[0]);
                        height = int.Parse(coords[1]);
                    }
                    line = sr.ReadLine();
                }*/
                string[] cores = line.Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach(string core in cores){
                    if(core == cores.Last()){ // if we are reading dimensions we want to split once more
                        string[] lastLine = core.Split('!', StringSplitOptions.RemoveEmptyEntries);
                        newCores.Add(InterpretCore(lastLine[0]));
                        width = int.Parse(lastLine[1]);
                        height = int.Parse(lastLine[2]);
                    }
                    else newCores.Add(InterpretCore(core));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        Console.WriteLine("REBUILD: " + width + " x " + height);

        LogInfo result = new LogInfo();
        result.cores = newCores;
        result.width = width;
        result.height = height + 1; //is this correct..?
        result.uniqueID = int.Parse(logFile.Split("_")[0]);

        return result;
    }

    private static GridCore InterpretCore(string coreInfo)
    {
        string[] info = coreInfo.Split(":");
        GridCore core = new GridCore(int.Parse(info[0]), int.Parse(info[1]));
        for (int i = 2; i < info.Length; i++)
        {
            int argb = int.Parse(info[i]);
            core.AddHistory(argb);
        }

        if (core.history.Count > 0) core.SetColor(Color.FromArgb(core.history.Last()));
        else Console.WriteLine("WARNING: Interpreted empty core");

        return core;
    }

    public struct LogInfo()
    {
        public List<GridCore> cores = new List<GridCore>();
        public int width;
        public int height;
        public int uniqueID;
    }
}