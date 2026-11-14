using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WinNvm
{
    internal static class NvmUtils
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Regex to match version folder names like "v14.17.0" or "v20.0.0-rc.1"
        private static readonly Regex VersionFolderRegex = new(@"^v(\d+)\.(\d+)\.(\d+)(?:-(\S+))?$", RegexOptions.Compiled);

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WinNvm/2.0");
            return client;
        }

        internal static string NormalizeVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new WinNvmException("Node version cannot be empty.");
            }

            var normalized = version.Trim();
            if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            {
                normalized = normalized.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(normalized) ||
                normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new WinNvmException($"Invalid Node version: '{version}'.");
            }

            return normalized;
        }

        internal static List<string> GetInstalledNodeVersions(string nvmHome)
        {
            var installedVersions = new List<string>();
            if (!Directory.Exists(nvmHome))
            {
                return installedVersions;
            }

            foreach (var dir in Directory.EnumerateDirectories(nvmHome))
            {
                var folderName = Path.GetFileName(dir);
                if (!string.IsNullOrEmpty(folderName) && VersionFolderRegex.IsMatch(folderName))
                {
                    installedVersions.Add(folderName);
                }
            }

            installedVersions.Sort(CompareVersionFolderNames);
            return installedVersions;
        }

        /// <summary>
        /// Compare two version folder names (e.g. "v14.17.0" vs "v20.0.0-rc.1") by semantic version.
        /// Returns negative if a &lt; b.
        /// </summary>
        internal static int CompareVersionFolderNames(string a, string b)
        {
            if (TryParseSemVer(a, out var va) && TryParseSemVer(b, out var vb))
            {
                int cmp = va.Major.CompareTo(vb.Major);
                if (cmp != 0) return cmp;
                cmp = va.Minor.CompareTo(vb.Minor);
                if (cmp != 0) return cmp;
                cmp = va.Patch.CompareTo(vb.Patch);
                if (cmp != 0) return cmp;

                if (va.Prerelease == null && vb.Prerelease == null) return 0;
                if (va.Prerelease == null) return 1;  // stable > prerelease
                if (vb.Prerelease == null) return -1;

                return ComparePrerelease(va.Prerelease, vb.Prerelease);
            }

            return StringComparer.OrdinalIgnoreCase.Compare(a, b);
        }

        private static bool TryParseSemVer(string folderName, out (int Major, int Minor, int Patch, string? Prerelease) result)
        {
            var match = VersionFolderRegex.Match(folderName);
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out int major) &&
                int.TryParse(match.Groups[2].Value, out int minor) &&
                int.TryParse(match.Groups[3].Value, out int patch))
            {
                var pre = match.Groups[4].Success ? match.Groups[4].Value : null;
                result = (major, minor, patch, pre);
                return true;
            }

            result = default;
            return false;
        }

        private static int ComparePrerelease(string a, string b)
        {
            var partsA = a.Split('.');
            var partsB = b.Split('.');
            int len = Math.Min(partsA.Length, partsB.Length);

            for (int i = 0; i < len; i++)
            {
                bool numA = int.TryParse(partsA[i], out int ia);
                bool numB = int.TryParse(partsB[i], out int ib);

                int cmp = (numA, numB) switch
                {
                    (true, true) => ia.CompareTo(ib),
                    (true, false) => -1, // numeric identifiers have lower precedence than non-numeric
                    (false, true) => 1,
                    _ => StringComparer.OrdinalIgnoreCase.Compare(partsA[i], partsB[i])
                };

                if (cmp != 0) return cmp;
            }

            return partsA.Length.CompareTo(partsB.Length);
        }

        private static void ExtractToNvmHome(string zipFileName, string verToInstall, string nvmHome)
        {
            Directory.CreateDirectory(nvmHome);

            var appPath = Path.Combine(nvmHome, "v" + verToInstall);
            var stagingPath = Path.Combine(nvmHome, $".tmp-v{verToInstall}-{Guid.NewGuid():N}");
            var stagingFullPath = Path.GetFullPath(stagingPath);
            if (!stagingFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                stagingFullPath += Path.DirectorySeparatorChar;
            }

            var rootFolderPrefix = "node-v" + verToInstall + GetArchSuffix();
            int fileCount = 0;

            try
            {
                Directory.CreateDirectory(stagingPath);

                using (var archive = ZipFile.OpenRead(zipFileName))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var relativePath = entry.FullName;
                        if (relativePath.StartsWith(rootFolderPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            relativePath = relativePath.Substring(rootFolderPrefix.Length).TrimStart('/', '\\');
                        }

                        if (string.IsNullOrEmpty(relativePath))
                            continue;

                        var destinationPath = Path.Combine(stagingPath, relativePath);
                        var fullDestPath = Path.GetFullPath(destinationPath);

                        if (!fullDestPath.StartsWith(stagingFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new WinNvmException($"Archive entry '{entry.FullName}' resolves outside target directory.");
                        }

                        if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                        {
                            Directory.CreateDirectory(fullDestPath);
                        }
                        else
                        {
                            var dir = Path.GetDirectoryName(fullDestPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            entry.ExtractToFile(fullDestPath, overwrite: true);
                            fileCount++;
                        }
                    }
                }

                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, recursive: true);
                }

                Directory.Move(stagingPath, appPath);
                Console.WriteLine($"Extracted {fileCount} files to {appPath}");
            }
            finally
            {
                if (Directory.Exists(stagingPath))
                {
                    try { Directory.Delete(stagingPath, recursive: true); } catch { /* best-effort cleanup */ }
                }
            }
        }

        private static string GetDownloadUrl(string nodeMirror, string verToInstall) =>
            $"{nodeMirror}v{verToInstall}/node-v{verToInstall}{GetFileName()}";

        private static string GetShaSumUrl(string nodeMirror, string verToInstall) =>
            $"{nodeMirror}v{verToInstall}/SHASUMS256.txt";

        private static string GetSavePath(string verToInstall) =>
            Path.Combine(Path.GetTempPath(), $"winnvm-v{verToInstall}-{Guid.NewGuid():N}.zip");

        private static string GetFileName() => GetArchSuffix() + ".zip";

        private static string GetArchSuffix()
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "-win-arm64",
                Architecture.X64 => "-win-x64",
                _ => Environment.Is64BitOperatingSystem ? "-win-x64" : "-win-x86"
            };
        }

        internal static void PrintVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0];
            var version = !string.IsNullOrEmpty(infoVersion) ? infoVersion : assembly.GetName().Version?.ToString();
            Console.WriteLine($"{assembly.GetName().Name} {version}");
        }

        internal static void PrintListOfInstalledNodeVersions(NvmEnvironment env)
        {
            var versions = GetInstalledNodeVersions(env.NvmHome);

            if (versions.Count == 0)
            {
                Console.WriteLine($"No Node versions found in {env.NvmHome}.");
                return;
            }

            var activeFolder = GetActiveVersionFolder(env);

            Console.WriteLine();
            Console.WriteLine("Installed Node versions:");
            Console.WriteLine();
            foreach (var version in versions)
            {
                bool isActive = string.Equals(version, activeFolder, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(isActive ? $"  * {version} (active)" : $"    {version}");
            }
        }

        private static string? GetActiveVersionFolder(NvmEnvironment env)
        {
            try
            {
                var linkInfo = new DirectoryInfo(env.NvmSymLink);
                var target = linkInfo.LinkTarget;
                if (string.IsNullOrEmpty(target))
                {
                    return null;
                }

                var fullTarget = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var nvmHomeFull = Path.GetFullPath(env.NvmHome).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (fullTarget.StartsWith(nvmHomeFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(fullTarget);
                }
            }
            catch
            {
                // Ignore if symlink cannot be inspected on current filesystem
            }

            return null;
        }

        internal static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: winnvm [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine(@"
    -i, --install <version>    To install a new version of NodeJS
    -l, --list                 To list all installed versions of NodeJS
    -u, --use <version>        To use the given version of NodeJS
    -r, --remove <version>     To uninstall a version of NodeJS
    -h, --help                 Show this message
    -v, --version              Display Version");
        }

        internal static RcFileData LoadRcJson()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var rcFile = Path.Combine(homePath, Constants.RcFileName);

            RcFileData? rcData = null;
            if (File.Exists(rcFile))
            {
                var json = File.ReadAllText(rcFile);
                rcData = JsonSerializer.Deserialize<RcFileData>(json, JsonOptions);
            }

            if (rcData == null || string.IsNullOrWhiteSpace(rcData.NodeMirror))
            {
                rcData = new RcFileData { NodeMirror = Constants.DefaultNodeMirror };
            }

            if (!rcData.NodeMirror.EndsWith('/'))
            {
                rcData.NodeMirror += "/";
            }

            return rcData;
        }

        private static string GetChecksumFile(string fileName)
        {
            using var fileStream = File.OpenRead(fileName);
            using var sha = SHA256.Create();
            var checksum = sha.ComputeHash(fileStream);
            return Convert.ToHexString(checksum);
        }

        private static void ValidateSha256Sum(string fileNameForSaving, string shaInfo)
        {
            ArgumentNullException.ThrowIfNull(shaInfo);
            var shaSumForFile = GetChecksumFile(fileNameForSaving);
            if (!shaInfo.Equals(shaSumForFile, StringComparison.OrdinalIgnoreCase))
            {
                throw new WinNvmException("SHA256 Checksum failed.");
            }
        }

        internal static async Task ValidateNodeVersionAndDownloadAsync(string verInput, RcFileData rcData, NvmEnvironment env)
        {
            var verToInstall = NormalizeVersion(verInput);
            var nodeMirror = rcData.NodeMirror ?? Constants.DefaultNodeMirror;
            var indexUrl = nodeMirror + "index.json";

            string json;
            try
            {
                using var response = await HttpClient.GetAsync(indexUrl);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new WinNvmException(
                        $"Cannot access {indexUrl}.\n" +
                        $"Please check the NodeMirror property in {Constants.RcFileName} in your home directory.\n" +
                        "Also check your network connection.");
                }

                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException exception)
            {
                throw new WinNvmException($"Failed to fetch Node versions: {exception.Message}", exception);
            }
            catch (TaskCanceledException exception)
            {
                throw new WinNvmException("Request timed out while fetching Node versions.", exception);
            }

            var versions = JsonSerializer.Deserialize<List<NodeVersions>>(json, JsonOptions) ?? new List<NodeVersions>();
            var targetTag = "v" + verToInstall;
            if (!versions.Any(v => string.Equals(v.Version, targetTag, StringComparison.OrdinalIgnoreCase)))
            {
                throw new WinNvmException($"Node version {verToInstall} is not available.");
            }

            var expectedArchiveName = $"node-v{verToInstall}{GetFileName()}";
            var shaSumsContent = await HttpClient.GetStringAsync(GetShaSumUrl(nodeMirror, verToInstall));
            var shaSums = shaSumsContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var theLine = shaSums.FirstOrDefault(line => line.TrimEnd().EndsWith(expectedArchiveName, StringComparison.OrdinalIgnoreCase));
            if (theLine == null)
            {
                throw new WinNvmException($"Checksum entry for '{expectedArchiveName}' was not found in SHASUMS256.txt.");
            }

            var shaInfo = Regex.Split(theLine.Trim(), @"\s+")[0];
            var urlToDownload = GetDownloadUrl(nodeMirror, verToInstall);
            var fileNameForSaving = GetSavePath(verToInstall);

            Console.WriteLine($"Downloading: {urlToDownload} -> {fileNameForSaving}");

            try
            {
                await using (var responseStream = await HttpClient.GetStreamAsync(urlToDownload))
                await using (var fileStream = new FileStream(fileNameForSaving, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await responseStream.CopyToAsync(fileStream);
                }

                Console.WriteLine("Downloaded. Verifying SHA256...");
                ValidateSha256Sum(fileNameForSaving, shaInfo);

                Console.WriteLine("Extracting...");
                ExtractToNvmHome(fileNameForSaving, verToInstall, env.NvmHome);
                Console.WriteLine($"Node v{verToInstall} installed successfully.");
            }
            finally
            {
                if (File.Exists(fileNameForSaving))
                {
                    try { File.Delete(fileNameForSaving); } catch { /* best-effort cleanup */ }
                }
            }
        }

        internal static NvmEnvironment ValidateEnvironment()
        {
            var nvmHome = Environment.GetEnvironmentVariable(Constants.NvmHomeVarName);
            var nvmSymLink = Environment.GetEnvironmentVariable(Constants.NvmSymLinkVarName);

            if (string.IsNullOrWhiteSpace(nvmHome))
            {
                throw new WinNvmException(
                    $"{Constants.NvmHomeVarName} is not defined. Please create an environment variable named {Constants.NvmHomeVarName}.");
            }

            if (string.IsNullOrWhiteSpace(nvmSymLink))
            {
                throw new WinNvmException(
                    $"{Constants.NvmSymLinkVarName} is not defined. Please create an environment variable named {Constants.NvmSymLinkVarName}.");
            }

            return new NvmEnvironment(
                nvmHome.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                nvmSymLink.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        internal static void ValidateNodeVersionAndUse(string verInput, NvmEnvironment env)
        {
            var verToUse = NormalizeVersion(verInput);
            var src = Path.Combine(env.NvmHome, "v" + verToUse);
            var dest = env.NvmSymLink;

            if (!Directory.Exists(src))
            {
                throw new WinNvmException($"Version {verToUse} is not installed.");
            }

            var destInfo = new DirectoryInfo(dest);
            if (destInfo.Exists || destInfo.LinkTarget != null)
            {
                destInfo.Delete();
            }

            var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            var arguments = $"/c mklink /J \"{dest}\" \"{src}\"";

            var psInfo = new ProcessStartInfo(cmd, arguments)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psInfo)
                ?? throw new WinNvmException("Failed to start cmd.exe to create directory junction.");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(5000))
            {
                throw new WinNvmException("Timed out waiting for mklink to complete.");
            }

            if (process.ExitCode != 0)
            {
                var details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new WinNvmException($"Failed to switch Node version (exit code {process.ExitCode}): {details.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine(stdout.Trim());
            }
            Console.WriteLine($"Now using Node v{verToUse}.");
        }

        internal static void UninstallNodeJs(string verInput, NvmEnvironment env)
        {
            var verToRemove = NormalizeVersion(verInput);
            var targetFolder = "v" + verToRemove;
            var src = Path.Combine(env.NvmHome, targetFolder);

            if (!Directory.Exists(src))
            {
                throw new WinNvmException($"Version {verToRemove} is not installed.");
            }

            var activeFolder = GetActiveVersionFolder(env);
            if (string.Equals(activeFolder, targetFolder, StringComparison.OrdinalIgnoreCase))
            {
                throw new WinNvmException(
                    $"Version {verToRemove} is currently active. Switch to another version with --use before removing it.");
            }

            Directory.Delete(src, recursive: true);
            Console.WriteLine($"Node {targetFolder} has been uninstalled.");
        }
    }
}
