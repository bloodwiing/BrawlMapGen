using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace BMG
{
    public static class Logger
    {
        private static OptionsBase options = new OptionsOld();
        public static TitleClass Title = new TitleClass();

        public static void UpdateOptions(OptionsBase options, string version)
        {
            Logger.options = options;

            Title.UpdateObjects(options.TitleOpts);
            Title.version = version;
            
            Console.Title = Title.GetAppInfo();
        }

        static List<string> loggedLines = new List<string>();

        public static void Log(string text) // Send a line to console + add to log
        {
            Console.WriteLine(text);
            loggedLines.Add(text);
        }

        public static void LogSpacer() // Empty line
        {
            Console.WriteLine();
            loggedLines.Add("");
        }

        public static bool CheckEventFlag(BMGEvent flag) => options.ConsoleOpts.EventFilter.HasFlag(flag);

        public enum AALDirection { In, Out }
        public static void LogAAL(AALDirection direction, string file) // Log AAL events
        {
            if (!CheckEventFlag(BMGEvent.AAL)) return;
            if (direction == AALDirection.In)
                Log(" [ AAL ] READ << " + file);
            else
                Log(" [ AAL ] WRITE >> " + file);
        }

        public static void LogStatus(string text) // Log status changes
        {
            if (!CheckEventFlag(BMGEvent.STATUS)) return;
            Log(" Status: " + text);
        }

        public static void LogSetup(string text, bool prefix = true) // Log setup jobs
        {
            if (!CheckEventFlag(BMGEvent.SETUP)) return;
            if (prefix)
                Log("New job: " + text);
            else
                Log(text);
        }

        public static void LogExport(string file) // Log setup jobs
        {
            if (!CheckEventFlag(BMGEvent.EXPORT)) return;
            LogSpacer();
            if (Regex.IsMatch(file, "\\S:"))
                Log("Image saved!\n  Location: " + file);
            else
                Log("Image saved!\n  Location: " + Path.GetFullPath("./" + file));
        }

        public enum TileEvent { tileDraw, gamemodeModding }

        //public static void LogTile(TileActionTypes tat, Tiledata.Tile tile, int y, int x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
        //{
        //    LogTile(tat, tile, y.ToString(), x.ToString(), yMax, xMax, tileEvent);
        //}

        //public static void LogTile(TileActionTypes tat, Tiledata.Tile tile, string y, string x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
        //{
        //    if (tileEvent == TileEvent.tileDraw && !CheckEventFlag(BMGEvent.DRAW)) return;
        //    if (tileEvent == TileEvent.gamemodeModding && !CheckEventFlag(BMGEvent.MOD)) return;

        //    Log(TileActionStringMaker(tat, tile, y, x, yMax, xMax));
        //}

        public static void LogWarning(string text, int timeout = 10) // Log a warning and pause
        {
            LogSpacer();
            Log(" WARNING: " + text);
            Log(string.Format(" Resuming in {0} seconds...", timeout));
            Thread.Sleep(timeout * 1000);
        }

        public static void LogError(Exception error) // Log an error
        {
            LogError(error.ToString());
        }

        public static void LogError(string error) // Log an error
        {
            LogSpacer();
            Log(" !! FATAL ERROR:\n" + error);
        }

        public class TitleClass
        {
            public JobClass Job;
            public StatusClass Status;
            public string StatusDetails;
            public string version;
            public bool show;
            public TitleOptionsBase options;

            public TitleClass()
            {
                Job = new JobClass();
                Status = new StatusClass();
                StatusDetails = "";
            }

            public void UpdateObjects(TitleOptionsBase options)
            {
                show = options != null;
                if (!show)
                    return;
                this.options = options;
                Job.options = options.Job;
                Status.options = options.Status;
            }

            public class JobClass
            {
                public int current;
                public int max;
                public string percentage;
                public string progressBar;
                public string jobsRatio;
                public string jobName;
                public TitleOptionsBase.JobBase options;

                public JobClass UpdateJob(int currentJobIndex, int maxJobIndex, string job = "")
                {
                    if (options == null)
                        return this;

                    current = currentJobIndex;
                    max = maxJobIndex;
                    jobName = job;
                    jobsRatio = LeftSpaceFiller(currentJobIndex, maxJobIndex.ToString().Length, ' ') + "/" + maxJobIndex;
                    percentage = Math.Floor(Convert.ToDouble(currentJobIndex) / Convert.ToDouble(maxJobIndex) * 100) + "%";
                    progressBar = MakeProgressBar(10, options.Full, options.Empty, Convert.ToDouble(currentJobIndex) / Convert.ToDouble(maxJobIndex));

                    return this;
                }

                public JobClass UpdateJobName(string job)
                {
                    jobName = job;

                    return this;
                }

                public JobClass IncreaseJob()
                {
                    if (options == null)
                        return this;

                    jobsRatio = LeftSpaceFiller(++current, max.ToString().Length, ' ') + "/" + max;
                    percentage = Math.Floor(Convert.ToDouble(current) / Convert.ToDouble(max) * 100) + "%";
                    progressBar = MakeProgressBar(10, options.Full, options.Empty, Convert.ToDouble(current) / Convert.ToDouble(max));

                    return this;
                }

                public override string ToString()
                {
                    return Utils.StringVariables(
                        options.Layout,
                        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "PERCENT", percentage },
                            { "BAR", progressBar },
                            { "JOB", jobName },
                            { "RATIO", jobsRatio }
                        }
                        );
                }
            }

            public string GetAppInfo()
            {
                if (options.AppInfo.ShowVersion)
                    return "BMG " + version;
                return "BMG";
            }

            public enum StatusDetailsType { basic, biome, tile }

            public void UpdateStatusDetails(string newDetails, StatusDetailsType type)
            {
                switch (type)
                {
                    case StatusDetailsType.basic:
                        StatusDetails = newDetails;
                        break;
                    case StatusDetailsType.biome:
                        if (options.StatusDetails.ShowBiome != false)
                            StatusDetails = newDetails;
                        break;
                    case StatusDetailsType.tile:
                        if (options.StatusDetails.ShowTile != false)
                            StatusDetails = newDetails;
                        break;
                }
            }

            public class StatusClass
            {
                public int current;
                public int max;
                public string percentage;
                public string progressBar;
                public string actionRatio;
                public string statusText;
                public TitleOptionsBase.StatusBase options;

                public StatusClass UpdateStatus(int currentActionIndex, int maxActionIndex, string action)
                {
                    if (options == null)
                        return this;

                    current = currentActionIndex;
                    max = maxActionIndex;
                    statusText = action;
                    actionRatio = LeftSpaceFiller(currentActionIndex, maxActionIndex.ToString().Length, ' ') + "/" + maxActionIndex;
                    percentage = Math.Floor(Convert.ToDouble(currentActionIndex) / Convert.ToDouble(maxActionIndex) * 100) + "%";
                    progressBar = MakeProgressBar(10, options.Full, options.Empty, Convert.ToDouble(currentActionIndex) / Convert.ToDouble(maxActionIndex));

                    return this;
                }

                public StatusClass IncreaseStatus()
                {
                    if (options == null)
                        return this;

                    actionRatio = LeftSpaceFiller(++current, max.ToString().Length, ' ') + "/" + max;
                    percentage = Math.Floor(Convert.ToDouble(current) / Convert.ToDouble(max) * 100) + "%";
                    progressBar = MakeProgressBar(10, options.Full, options.Empty, Convert.ToDouble(current) / Convert.ToDouble(max));

                    return this;
                }

                public override string ToString()
                {
                    return Utils.StringVariables(
                        options.Layout,
                        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "PERCENT", percentage },
                            { "BAR", progressBar },
                            { "STATUS", statusText },
                            { "RATIO", actionRatio }
                        }
                        );
                }
            }

            public override string ToString()
            {
                return Utils.StringVariables(
                    options.Layout,
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "APP", GetAppInfo() },
                        { "JOB", Job.ToString() },
                        { "STATUS", Status.ToString() },
                        { "DETAILS", StatusDetails }
                    }
                    );
            }

            public void RefreshTitle()
            {
                if (show && options.UpdateEnabled)
                    return;

                Console.Title = ToString();
            }
        }

        public static void Save(string fileName) // Save log file
        {
            if (options.SaveLog)
            {
                if (CheckEventFlag(BMGEvent.AAL))
                    Console.WriteLine(" [ AAL ] WRITE >> " + "./" + fileName);
                File.WriteAllText(fileName, string.Join("\n", loggedLines).Replace("\n", Environment.NewLine));
                Console.WriteLine("\nLog saved!\n  Location: " + Path.GetFullPath("./" + fileName));
            }
            else
                Console.WriteLine("\nLog saving is disabled.");
        }

        public static string LeftSpaceFiller(string text, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = text.ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return t + text;
            }
            return text;
        }

        public static string LeftSpaceFiller(int number, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = number.ToString().ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return t + number;
            }
            return number.ToString();
        }

        public static string RightSpaceFiller(string text, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = text.ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return text + t;
            }
            return text;
        }

        public static string RightSpaceFiller(int number, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = number.ToString().ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return number + t;
            }
            return number.ToString();
        }

        public static string MakeProgressBar(int lenght, char fill, char background, double percent, bool leftToRight = true) // AMTool: Text progress bars
        {
            string prog = "";
            for (int x = 0; x < Math.Floor(lenght * percent); x++)
                prog += fill;
            if (leftToRight)
                return RightSpaceFiller(prog, lenght, background);
            else
                return LeftSpaceFiller(prog, lenght, background);
        }

        //public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, int yLocation, int xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        //{
        //    return TileActionStringMaker(tat, tile, yLocation.ToString(), xLocation.ToString(), yLocationMax, xLocationMax);
        //}

        //public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, string yLocation, string xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        //{
        //    string p;
        //    string t;
        //    string n = tile.tileName.ToUpper();

        //    if (tat.m) p = "m"; else p = " ";
        //    if (tat.g) p += "g"; else p += " ";
        //    if (tat.s) p += "s"; else p += " ";
        //    if (tat.o) p += "o"; else p += " ";
        //    if (tat.h) p += "h"; else p += " ";
        //    if (tat.d) p += "d"; else p += " ";

        //    if (tat.m)
        //    {
        //        if (tat.d)
        //            t = "DRAWN METADATA TILE AS \"" + n + "\".";
        //        else
        //            t = "REGISTERED METADATA TILE \"" + n + "\".";
        //    }
        //    else if (tat.g)
        //    {
        //        if (tat.o)
        //            t = "MODIFIED TO \"" + n + "\".";
        //        else
        //            t = "DRAWN AS \"" + n + "\".";
        //    }
        //    else if (tat.s)
        //    {
        //        if (tat.o)
        //        {
        //            if (tat.h)
        //            {
        //                if (tat.d)
        //                    t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
        //                else
        //                    t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING (SPECIAL TILE RULES).";
        //            }
        //            else
        //            {
        //                if (tat.d)
        //                    t = "DRAWN ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
        //                else
        //                    t = "\"" + n + "\" DELAYED FOR ORDERING (SPECIAL TILE RULES).";
        //            }
        //        }
        //        else
        //        {
        //            if (tat.d)
        //                t = "DRAWN AS \"" + n + "\" (SPECIAL TILE RULES).";
        //            else
        //                t = "SKIPPED.";
        //        }
        //    }
        //    else if (tat.o)
        //    {
        //        if (tat.h)
        //        {
        //            if (tat.d)
        //                t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\".";
        //            else
        //                t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING.";
        //        }
        //        else
        //        {
        //            if (tat.d)
        //                t = "DRAWN ORDERED TILE AS \"" + n + "\".";
        //            else
        //                t = "\"" + n + "\" DELAYED FOR ORDERING.";
        //        }
        //    }
        //    else
        //        t = "DRAWN AS \"" + n + "\"";

        //    return p + " [" + tile.tileCode + "] < y: " + LeftSpaceFiller(yLocation, yLocationMax.ToString().ToCharArray().Length, ' ') + " / x: " + LeftSpaceFiller(xLocation, xLocationMax.ToString().ToCharArray().Length, ' ') + " > " + t;
        //}
    }
}
