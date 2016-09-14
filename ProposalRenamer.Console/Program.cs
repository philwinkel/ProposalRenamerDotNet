using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProposalRenamer.Console
{
    class Program
    {
        const string source = "L:\\Proposals\\Cosential\\P# Proposal Files Upload\\Original Files 08082016\\";
        const string dest = "L:\\Proposals\\Cosential\\P# Proposal Files Upload\\Renamed Files\\";
        static Regex regex = new Regex("(.*)(P(?:MD)?)([0-9]{4,6})");

        static void Main(string[] args)
        {
            var files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            var matchFiles = new List<MatchFile>();
            var noMatchFiles = new List<string>();

            // go through files, split into matching and non-matching 
            foreach (var file in files)
            {
                Match match = regex.Match(file);
                if (!match.Success)
                {
                    noMatchFiles.Add(file);
                    continue;
                }

                matchFiles.Add(new MatchFile
                {
                    Filename = file,
                    Date = match.Groups[0].Value,
                    Type = match.Groups[1].Value,
                    Proposal = match.Groups[2].Value,
                    Ext = Path.GetExtension(file)
                });
            }
            
            System.Console.WriteLine($"Found {matchFiles.Count} match files! There were {noMatchFiles.Count} files that did not match.");

            // process match files
            foreach (var mf in matchFiles)
            {
                var destPath = dest;
                if (mf.Type == "P")
                    destPath += "Virginia\\";
                else if (mf.Type == "PMD")
                    destPath += "Maryland\\";

                var newFileName = mf.Type + mf.Proposal + mf.Ext;
                var newFilePath = Path.Combine(destPath, newFileName);

                File.Copy(mf.Filename, newFilePath);
                System.Console.WriteLine($"Copied {mf.Filename} to {newFilePath}");
            }

            // copy no match files
            foreach (var file in noMatchFiles)
            {
                var destPath = Path.Combine(dest + "\\Manual Rename Required\\" + Path.GetFileName(file));
                File.Copy(file, destPath);
                System.Console.WriteLine($"Copied {Path.GetFileName(file)} to {destPath}");
            }
        }
    }
}
