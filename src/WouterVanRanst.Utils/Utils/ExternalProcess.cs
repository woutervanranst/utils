﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WouterVanRanst.Utils.Extensions;

namespace WouterVanRanst.Utils;

public static class ExternalProcess
{
    /// <summary>
    /// Find a file systemwide
    /// </summary>
    /// <param name="windowsExecutableName">The Windows executable name, including the '.exe' suffix</param>
    /// <param name="linuxExecutableName">The Linux executable name</param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static string FindFullName(string windowsExecutableName, string linuxExecutableName, ILogger logger = default)
    {
        logger?.LogDebug($"Looking for windows:{windowsExecutableName} / linux:{linuxExecutableName}");
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            logger?.LogWarning("Environment variable PATH not found");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 1. Find in the PATH folders
            if (!string.IsNullOrEmpty(path))
            {
                var executables = path.Split(';')
                    .Where(dir => !string.IsNullOrWhiteSpace(dir))
                    .Select(dir => new DirectoryInfo(dir))
                    .SelectMany(dir => dir.TryGetFiles(windowsExecutableName))
                    .ToArray();

                if (executables.Length > 0)
                {
                    var fullname = executables.First().FullName;

                    logger?.LogDebug($"Found {executables.Length} instance(s) of {windowsExecutableName}. Returning the first one: {fullname}");

                    return fullname;
                }
            }

            // 2. Find using WHERE
            path = Path.GetPathRoot(Environment.SystemDirectory);

            try
            {
                logger?.LogWarning($"Did not find {windowsExecutableName} in PATH variable. Searching on {path}. Consider adding the location to the PATH variable to improve speed.");
                var fullNames = RunSimpleProcess("where", $" /R {path} {windowsExecutableName}").Split(Environment.NewLine);

                logger?.LogDebug($"Found {fullNames.Length} instance(s) of {windowsExecutableName}. Returning the first one: {fullNames.First()}");

                return fullNames.First();
            }
            catch (ApplicationException e)
            {
                throw new ArgumentException($"Could not find {windowsExecutableName} in {path}", nameof(windowsExecutableName), e); //TODO Karl this should terminate the application flow
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // 1. Find using WHICH
            // https://ss64.com/bash/which.html -- alternatief https://ss64.com/bash/whereis.html

            try
            {
                var fullNames = RunSimpleProcess("which", $"{linuxExecutableName}").Split(Environment.NewLine);

                logger?.LogDebug($"Found {fullNames.Length} instance(s) of {linuxExecutableName}. Returning the first one: {fullNames.First()}");

                return fullNames.First();
            }
            catch (ApplicationException e)
            {
                throw new ArgumentException($"Could not find {linuxExecutableName} in /", nameof(linuxExecutableName), e);
            }
        }
        else
            throw new NotSupportedException($"{RuntimeInformation.OSDescription} is not supported");
    }

    public static string RunSimpleProcess(string fileName, string arguments, DirectoryInfo? workingDirectory = null)
    {
        // https://github.com/Nicholi/OpenSSLCompat/blob/0e682c7b86e25bb219b742792afc839b21f44e44/OpenSSLCompat/Program.cs#L137

        using var process = new Process();

        var errorMsg = string.Empty;
        var output = string.Empty;

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.StartInfo.FileName  = fileName; // TODO use cmd.exe and pass on the exe name in the arguments
        process.StartInfo.Arguments = arguments;
        if (workingDirectory is not null)
            process.StartInfo.WorkingDirectory = workingDirectory.FullName;

        process.OutputDataReceived += (_, data) => output += data.Data + Environment.NewLine;
        process.ErrorDataReceived += (_, data) => errorMsg += data.Data ?? string.Empty;

        var cmdString = $"{process.StartInfo.FileName} {process.StartInfo.Arguments}";

        var started = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit(); // todo refactor for async

        if (process.ExitCode != 0)
            throw new ApplicationException($"Error in process execution {fileName} {arguments}: {(string.IsNullOrEmpty(errorMsg) ? output : errorMsg)}");

        return output;
    }
}