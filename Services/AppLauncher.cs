using System.ComponentModel;
using System.Diagnostics;
using FloatingToolbar.Models;
using Microsoft.Win32;

namespace FloatingToolbar.Services;

public sealed class LaunchResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public static LaunchResult Ok() => new() { Success = true };

    public static LaunchResult Fail(string message) => new() { Success = false, Error = message };
}

/// <summary>
/// Resolves targets through the Windows App Paths registry, PATH and the shell, then launches
/// them with ShellExecute. Missing applications produce a message instead of an exception.
/// </summary>
public static class AppLauncher
{
    public static LaunchResult Launch(LauncherItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Target))
        {
            return LaunchResult.Fail("This button does not have a target yet. Right-click it and choose an application.");
        }

        var resolved = Resolve(item.Target);
        var expanded = Environment.ExpandEnvironmentVariables(item.Target.Trim().Trim('"'));

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = resolved ?? expanded
        };

        if (!string.IsNullOrWhiteSpace(item.Arguments))
        {
            startInfo.Arguments = item.Arguments;
        }

        if (resolved is not null)
        {
            var directory = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(directory)) startInfo.WorkingDirectory = directory;
        }

        try
        {
            var process = Process.Start(startInfo);
            process?.Dispose();
            return LaunchResult.Ok();
        }
        catch (Win32Exception ex)
        {
            return LaunchResult.Fail(
                $"Windows could not start \u201c{item.Name}\u201d.\n\n" +
                $"Target: {item.DisplayTarget}\n\n{ex.Message}");
        }
        catch (Exception ex)
        {
            return LaunchResult.Fail(
                $"Something went wrong while starting \u201c{item.Name}\u201d.\n\n{ex.Message}");
        }
    }

    /// <summary>Returns a concrete executable path when one can be resolved, otherwise null.</summary>
    public static string? Resolve(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        var expanded = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));

        if (File.Exists(expanded)) return expanded;

        if (expanded.Contains("://", StringComparison.Ordinal) ||
            expanded.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) ||
            expanded.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Path.IsPathRooted(expanded)) return null;

        return FindInAppPaths(expanded) ?? FindOnPath(expanded);
    }

    public static bool TryReveal(LauncherItem item)
    {
        try
        {
            var path = Resolve(item.Target) ?? (File.Exists(item.Target) ? item.Target : null);
            if (path is null) return false;

            var startInfo = new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true };
            var process = Process.Start(startInfo);
            process?.Dispose();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? FindInAppPaths(string fileName)
    {
        var keyName = fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".exe";
        var hiveViews = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Registry64),
            (RegistryHive.CurrentUser, RegistryView.Registry32)
        };

        foreach (var (hive, view) in hiveViews)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{keyName}");
                if (key?.GetValue(null) is string value && !string.IsNullOrWhiteSpace(value))
                {
                    var path = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
                    if (File.Exists(path)) return path;
                }
            }
            catch (Exception)
            {
                // Missing keys and access errors simply mean "not found here".
            }
        }

        return null;
    }

    private static string? FindOnPath(string fileName)
    {
        var searchNames = fileName.Contains('.') ? new[] { fileName } : new[] { fileName, fileName + ".exe" };
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in searchNames)
            {
                try
                {
                    var candidate = Path.Combine(directory.Trim(), name);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (Exception)
                {
                    // Ignore malformed PATH entries.
                }
            }
        }

        return null;
    }
}
