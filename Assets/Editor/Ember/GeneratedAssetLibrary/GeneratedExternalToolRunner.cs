using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedExternalToolRunner
    {
        public static GeneratedExternalToolResult Run(GeneratedExternalToolCommand command, IDictionary<string, string> tokens, int timeoutSeconds)
        {
            var result = new GeneratedExternalToolResult();
            var arguments = Expand(command.argumentsTemplate, tokens);
            result.commandLine = command.executablePath + " " + arguments;

            if (string.IsNullOrWhiteSpace(command.executablePath))
            {
                result.stderr = "Executable path is not configured.";
                return result;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = command.executablePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = string.IsNullOrWhiteSpace(command.workingDirectory) ? Directory.GetCurrentDirectory() : command.workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                if (!process.WaitForExit(timeoutSeconds * 1000))
                {
                    try { process.Kill(); } catch { }
                    result.stderr = "Process timed out.";
                    return result;
                }

                result.exitCode = process.ExitCode;
                result.stdout = process.StandardOutput.ReadToEnd();
                result.stderr = process.StandardError.ReadToEnd();
                result.success = process.ExitCode == 0;
            }

            return result;
        }

        public static string WriteCommandPreview(string previewPath, GeneratedExternalToolCommand command, IDictionary<string, string> tokens)
        {
            var text = command.executablePath + " " + Expand(command.argumentsTemplate, tokens);
            Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
            File.WriteAllText(previewPath, text);
            return text;
        }

        private static string Expand(string template, IDictionary<string, string> tokens)
        {
            var result = template ?? string.Empty;
            if (tokens == null) return result;
            foreach (var pair in tokens)
                result = result.Replace(pair.Key, pair.Value ?? string.Empty);
            return result;
        }
    }
}
