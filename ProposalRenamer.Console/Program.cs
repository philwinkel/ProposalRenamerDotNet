using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using NLog.Internal;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace ProposalRenamer.Console
{
    class Program
    {
        const string dest = "L:\\Proposals\\Cosential\\P# Proposal Files Upload\\Britt\\Latest P# October 2016\\Renamed Files";
        static Regex regex = new Regex("(.*)(P(?:MD)?)([0-9]{4,6}.*\\.)");
        public static Logger logger => LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var source = ConfigurationManager.AppSettings["SourcePath"];
            logger.Trace($"SourcePath = {source}");

            var dest = ConfigurationManager.AppSettings["DestinationPath"];
            logger.Trace($"DestPath = {dest}");

            if (source == dest)
            {
                logger.Info("App.config error - source path and destination path are the same! press any key to exit");
                return;
            }

            var regex = new Regex(ConfigurationManager.AppSettings["MatchRegexPattern"]);
            logger.Trace($"Regex match pattern = ${ConfigurationManager.AppSettings["MatchRegexPattern"]}");

            logger.Trace($"Searching for files in {source}");
            var files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            logger.Trace($"Found {files.Length} files in {source}!");

            var matchFiles = new List<MatchFile>();
            var noMatchFiles = new List<string>();
            var marylandNoMatchFiles = new List<string>();
            var virginiaNoMatchFiles = new List<string>();

            // go through files, split into matching and non-matching 
            logger.Trace("Looking at files...");
            foreach (var file in files)
            {
                Match match = regex.Match(file);
                if (!match.Success)
                {
                    logger.Trace($"NO MATCH: {file}");
                    noMatchFiles.Add(file);

                    if (file.Contains("Maryland"))
                        marylandNoMatchFiles.Add(file);
                    else if (file.Contains("Virginia"))
                        virginiaNoMatchFiles.Add(file);

                    continue;
                }

                logger.Trace($"MATCH: {file}");
                for(var i = 0; i < match.Groups.Count; i++)
                {
                    logger.Trace($"{i} = {match.Groups[i].Value}");
                }
                matchFiles.Add(new MatchFile
                {
                    Filename = file,
                    Date = match.Groups[0].Value,
                    Type = match.Groups[2].Value,
                    Proposal = match.Groups[3].Value + match.Groups[4].Value,
                    Ext = Path.GetExtension(file)
                });
            }
            
            logger.Info($"Found {matchFiles.Count} match files! There were {noMatchFiles.Count} files that did not match.");

            logger.Info($"Maryland no match files: {marylandNoMatchFiles.Count}");
            logger.Info($"Virginia no match files: {virginiaNoMatchFiles.Count}");

            System.Console.WriteLine("Push any key to proceed with copying + renaming files.");
            System.Console.ReadKey();

            // process match files
            logger.Info("Copying match files to new directory...");
            foreach (var mf in matchFiles)
            {
                var destPath = $"{dest}";
                if (mf.Type == "P")
                    destPath += "Virginia\\";
                else if (mf.Type == "PMD")
                    destPath += "Maryland\\";

                var newFileName = mf.Type + mf.Proposal;
                var newFilePath = Path.Combine(destPath, newFileName);

                File.Copy(mf.Filename, newFilePath, true);
                logger.Info($"Copied {mf.Filename} to {newFilePath}");
            }

            // copy no match files
            logger.Info("Copying maryland no match files...");
            List<string> filesFailedToCopy = new List<string>();
            int copyErrors = 0;
            foreach (var file in marylandNoMatchFiles)
            {
                var destPath = Path.Combine(dest + "\\Maryland\\Files with no P#\\" + Path.GetFileName(file));
                logger.Info($"Copied {file} to {destPath}");
                try
                {
                    File.Copy(file, destPath, true);

                }
                catch (Exception e)
                {
                    logger.Error(e);
                    filesFailedToCopy.Add(file);
                    copyErrors++;
                }
            }
            logger.Info("Copying virginia no match files...");
            foreach (var file in virginiaNoMatchFiles)
            {
                var destPath = Path.Combine(dest + "\\Virginia\\Files with no P#\\" + Path.GetFileName(file));
                logger.Info($"Copying {file} to {destPath}");
                try
                {
                    File.Copy(file, destPath, true);

                }
                catch (Exception e)
                {
                    logger.Error(e);
                    filesFailedToCopy.Add(file);
                    copyErrors++;
                }
            }
            logger.Info("Done");
            if (copyErrors > 0)
            {
                logger.Error("There were {copyErrors} errors, could not copy the following files:");
                foreach(var f in filesFailedToCopy)
                    logger.Info(f);
            }
        }
    }
}
