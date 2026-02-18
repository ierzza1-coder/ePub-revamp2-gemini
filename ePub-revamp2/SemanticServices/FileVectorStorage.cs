using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ePub2.SemanticServices
{
    public static class FileVectorStorage
    {
        public static void Save(string filePath, Dictionary<int, Dictionary<string, double>> docVectors, Dictionary<string, double> idf)
        {
            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine("#IDF");
                foreach (var kv in idf)
                    sw.WriteLine($"{kv.Key}\t{kv.Value}");

                sw.WriteLine("#DOCS");
                foreach (var kv in docVectors)
                {
                    var line = kv.Key + " " + string.Join(" ", kv.Value.Select(v => v.Key + ":" + v.Value.ToString("F6")));
                    sw.WriteLine(line);
                }
            }
        }


        public static (Dictionary<int, Dictionary<string, double>>, Dictionary<string, double>) Load(string filePath)
        {
            var idf = new Dictionary<string, double>();
            var docVectors = new Dictionary<int, Dictionary<string, double>>();
            var lines = File.ReadAllLines(filePath);
            int mode = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("#IDF")) { mode = 0; continue; }
                if (line.StartsWith("#DOCS")) { mode = 1; continue; }
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (mode == 0)
                {
                    var parts = line.Split('\t');
                    if (parts.Length == 2 && double.TryParse(parts[1], out double val))
                        idf[parts[0]] = val;
                }
                else
                {
                    // Split by first space manually
                    int index = line.IndexOf(' ');
                    if (index < 0)
                    {
                        // No space found, skip line
                        continue;
                    }

                    string firstPart = line.Substring(0, index);
                    string secondPart = line.Substring(index + 1);

                    var spaceParts = new string[] { firstPart, secondPart };


                    if (spaceParts.Length != 2) continue;
                    int docId = int.Parse(spaceParts[0]);
                    var vector = new Dictionary<string, double>();
                    foreach (var wv in spaceParts[1].Split(' '))
                    {
                        var kv = wv.Split(':');
                        if (kv.Length == 2 && double.TryParse(kv[1], out double val))
                            vector[kv[0]] = val;
                    }
                    docVectors[docId] = vector;
                }
            }
            return (docVectors, idf);
        }
    }
}
