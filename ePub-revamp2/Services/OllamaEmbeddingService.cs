using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;


namespace ePub_revamp2.Services
{
    public class OllamaEmbeddingService
    {
        public float[] GetEmbeddingFromOllamaCLI(string text)
        {
            // paste the method body here
            var startInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = $"deepseek-embed \"{text.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // ... use output here or return it

                var match = Regex.Match(output, @"\[(.*?)\]");
                if (!match.Success)
                    throw new Exception("Embedding output not found: " + output);

                var vectorParts = match.Groups[1].Value.Split(',');
                return vectorParts.Select(p => float.Parse(p.Trim())).ToArray();
            }
        }
    }
}
