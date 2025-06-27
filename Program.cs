using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static GraphMolWrap.RDKit;
using GraphMolWrap;

class Program
{
    static void Main(string[] args)
    {
        // Set up RDKit environment (required for .NET RDKit)
        //GraphMolWrap.Chem.Initialize();

        // Get the current working directory (where the .exe is located)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string sdfDir = Path.Combine(baseDir, "sdf");
        string outDir = Path.Combine(baseDir, "out");

        // Check if sdf directory exists
        if (!Directory.Exists(sdfDir))
        {
            Console.WriteLine($"Error: Directory '{sdfDir}' does not exist.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        // Prompt user to process SDF files
        Console.WriteLine($"Process all SDF files in '{sdfDir}'? (y/n)");
        string response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y")
        {
            Console.WriteLine("Operation cancelled. Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Get all .sdf files
        var sdfFiles = Directory.GetFiles(sdfDir, "*.sdf").ToList();
        if (sdfFiles.Count == 0)
        {
            Console.WriteLine($"No SDF files found in '{sdfDir}'. Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Found {sdfFiles.Count} SDF file(s). Starting processing...");

        // Process each SDF file
        for (int i = 0; i < sdfFiles.Count; i++)
        {
            string sdfFile = sdfFiles[i];
            string fileName = Path.GetFileNameWithoutExtension(sdfFile);
            string outFile = Path.Combine(outDir, $"{fileName}.txt");

            // Process SDF file and extract SMILES
            try
            {
                List<string> smilesList = new List<string>();
                using (var reader = new SDMolSupplier(sdfFile))
                {
                    while (!reader.atEnd())
                    {
                        using (var mol = reader.next())
                        {
                            if (mol != null)
                            {
                                string smiles = RDKFuncs.MolToSmiles(mol);
                                if (!string.IsNullOrEmpty(smiles))
                                {
                                    smilesList.Add(smiles);
                                }
                            }
                        }
                    }
                }

                // Save SMILES to output file
                File.WriteAllLines(outFile, smilesList);
                Console.WriteLine($"Processed '{fileName}.sdf': {smilesList.Count} molecules written to '{outFile}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{fileName}.sdf': {ex.Message}");
            }

            // Display progress bar
            int progress = (i + 1) * 100 / sdfFiles.Count;
            int barLength = 10;
            int filled = progress / 10;
            string bar = "[" + new string('=', filled) + ">" + new string(' ', barLength - filled) + $"] {progress}%";
            Console.WriteLine(bar);
        }

        Console.WriteLine("Processing completed. Press any key to exit...");
        Console.ReadKey();
    }
}