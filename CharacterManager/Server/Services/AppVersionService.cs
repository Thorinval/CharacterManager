using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CharacterManager.Server.Services;

public class AppVersionService
{
    private readonly IConfiguration _configuration;

    public AppVersionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetAppName()
    {
        return _configuration["AppInfo:Name"] ?? "Character Manager";
    }

    public string GetAppVersion()
    {
        return _configuration["AppInfo:Version"] ?? "0.1.0";
    }

    public string GetAuthor()
    {
        return _configuration["AppInfo:Author"] ?? "Unknown";
    }

    public string GetDescription()
    {
        return _configuration["AppInfo:Description"] ?? "";
    }

    public string GetBuildVersion()
    {
        try
        {
            // Essayer de récupérer le nombre de commits git
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-list --count HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (int.TryParse(output, out var commitCount))
            {
                return $"Build {commitCount}";
            }
        }
        catch
        {
            // Si git n'est pas disponible ou erreur, utiliser la date de build
        }

        // Fallback: utiliser la date de build de l'assembly
        var assembly = Assembly.GetExecutingAssembly();
        var fileInfo = new FileInfo(assembly.Location);
        return $"Build {fileInfo.LastWriteTime:yyyyMMdd}";
    }

    public string GetGitCommitHash()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return output;
        }
        catch
        {
            return "unknown";
        }
    }

    public string GetFullVersionString()
    {
        var appVersion = GetAppVersion();
        var buildVersion = GetBuildVersion();
        var commitHash = GetGitCommitHash();
        
        return $"{appVersion} ({buildVersion}, {commitHash})";
    }
}