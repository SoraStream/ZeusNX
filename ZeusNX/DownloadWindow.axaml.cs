using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;
using ZeusNX.Ini;
using ZeusNX.YoYoMD5;

namespace ZeusNX;

public interface IRuntimeItem
{
    public string DisplayVersion { get; }
    public string DisplaySubtitle { get; }
    public bool IsInstalled { get; set; }
}
public class YYRuntimeMetadata : IRuntimeItem
{
    public string Version { get; set; }
    public string Date { get; set; }
    public string ReleaseNotesURL { get; set; }
    public string BaseURL { get; set; }
    public string WinBaseURL { get; set; }
    public string OSXx64BaseUrl  { get; set; }
    public string OSXarm64BaseUrl { get; set; }
    public string Linuxx64BaseUrl { get; set; }
    public string Linuxarm64BaseUrl { get; set; }
    public string ZnxUrl { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsZNXInstalled { get; set; }

    public string DisplayVersion => Version;
    public string DisplaySubtitle => Date;
}

public class ZNXRuntimeMetadata : IRuntimeItem
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("date")]
    public string Date { get; set; }
    [JsonProperty("size")]
    public string Size { get; set; }
    [JsonProperty("note")]
    public string Note { get; set; }
    [JsonProperty("file")]
    public string File { get; set; }
    [JsonProperty("private")]
    public bool Private {  get; set; }
    public bool IsInstalled { get; set; }

    public string DisplayVersion => Name;
    public string DisplaySubtitle => Date;
}

public partial class DownloadWindow : Window
{
    private readonly MainWindow? _window;
    string cachePath = Path.Combine("Data", "Cache");
    bool init = false;
    public DownloadWindow(MainWindow window)
    {
        InitializeComponent();
        _window = window;
        //var trace = traceAction;
        LoadRuntimes(0);
        init = true;
    }

    //0 is monthly, 1 is lts and 2 will be spooky scary zeusnx runtimes
    private async void LoadRuntimes(int runtype)
    {
        LoadingArea.IsVisible = true;
        DetailsArea.IsVisible = false;
        try
        {
            using var client = new HttpClient();
            byte[] byteStream;
            string xml = String.Empty;
            var list = new List<YYRuntimeMetadata>();
            var ini = new IniFile();
            ini.Load(Path.Combine("Data", "config.ini"));
            bool usePD = ini["Config"]["UseProgramData"].ToBool();

            switch (runtype)
            {
                case 0:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime.rss");
                    break;
                case 1:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime-LTS.rss");
                    break;
                case 2:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime-LTS2026.rss");
                    break;
                case 3:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime-NuBeta.rss");
                    break;
            }
            //get zeusnx feed for displaying shit
            string znxJson = await client.GetStringAsync("https://sorastream.dev/zeusnx/files.json");
            bool priv = false;
            List<ZNXRuntimeMetadata> znxFeed = JsonConvert.DeserializeObject<List<ZNXRuntimeMetadata>>(znxJson);

            //isinstalled stuff
            string rp = string.Empty;
            string runPath = string.Empty;
            if (!usePD)
            {
                runPath = runtype switch
                {
                    0 => "monthly",
                    1 => "lts",
                    2 => "lts2026",
                    3 => "beta"
                };
            }
            else
            {
                runPath = runtype switch
                {
                    0 => "",
                    1 => "-LTS",
                    2 => "-LTS2026",
                    3 => "-Beta"
                };
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("sparkle", "http://www.andymatuschak.org/xml-namespaces/sparkle");

            var items = doc.GetElementsByTagName("item");

            foreach (XmlNode item in items)
            {
                //right depending on the runtime the base-module stuff doesn't exist, basically anything pre 2023.2. lts is infact, pre 2023.2.
                string title = item["title"]?.InnerText.Replace("Version ", "");
                //check if there's a znx runtime that matches
                string znxUrl = "";
                foreach (var thing in znxFeed)
                {
                    if (title == thing.DisplayVersion)
                    {
                        znxUrl = $"https://sorastream.dev/zeusnx/{thing.File}";
                        priv = thing.Private;
                    }
                }
                var enclosure = item.SelectSingleNode("enclosure");
                if (znxUrl == "" || priv || enclosure == null) continue;
                string WinBase = string.Empty, OSXx64Base = string.Empty, OSXarm64Base = string.Empty, Linuxx64Base = string.Empty, Linuxarm64Base = string.Empty;
                if (Int32.Parse(title.Split('.')[0]) >= 2023)
                {
                    if (!title.Contains("2023.1"))
                    {
                        WinBase = enclosure.SelectSingleNode("module[@name='base-module-windows-x64']").Attributes["url"]?.Value;
                        OSXx64Base = enclosure.SelectSingleNode("module[@name='base-module-osx-x64']").Attributes["url"]?.Value;
                        OSXarm64Base = enclosure.SelectSingleNode("module[@name='base-module-osx-arm64']").Attributes["url"]?.Value;
                        Linuxx64Base = enclosure.SelectSingleNode("module[@name='base-module-linux-x64']").Attributes["url"]?.Value;
                        Linuxarm64Base = enclosure.SelectSingleNode("module[@name='base-module-linux-arm64']").Attributes["url"]?.Value;
                    }
                }
                string rawDate = item["pubDate"]?.InnerText ?? "";
                DateTime cookedDate;
                DateTime.TryParse(rawDate, out cookedDate);
                string cleanDate = $"{cookedDate.Year}-{(cookedDate.Month <= 9 ? "0" + cookedDate.Month : cookedDate.Month)}-{(cookedDate.Day <= 9 ? "0" + cookedDate.Day : cookedDate.Day)}"; //DateTime.TryParse(rawDate, out var dt) ? dt.ToShortDateString() : rawDate;
                if (!File.Exists(Path.Combine(cachePath, $"release-notes-{title}.json")))
                {
                    byteStream = await client.GetByteArrayAsync(item["comments"]?.InnerText ?? "");
                    File.WriteAllBytesAsync(Path.Combine(cachePath, $"release-notes-{title}.json"), byteStream);
                }
                string endingDir = Path.Combine($"GameMakerStudio2{runPath}", "Cache", "runtimes", $"runtime-{title}");
                switch (MainWindow.platform)
                {
                    case "windows":
                        rp = Path.Combine("C:", "ProgramData", endingDir);
                        break;
                    case "osx":
                        rp = "/" + Path.Combine("Users", "Shared", endingDir);
                        break;
                    case "linux":
                        rp = Path.Combine("Runners", "gm" + runPath, $"runtime-{title}");
                        break;
                }

                if (!usePD)
                    rp = Path.Combine("Runners", "gm" + runPath, $"runtime-{title}");

                list.Add(new YYRuntimeMetadata
                {
                    Version = item["title"]?.InnerText.Replace("Version ", ""),
                    Date = cleanDate,
                    BaseURL = enclosure.Attributes["url"]?.Value,
                    WinBaseURL = WinBase,
                    OSXx64BaseUrl =  OSXx64Base,
                    OSXarm64BaseUrl =  OSXarm64Base,
                    Linuxx64BaseUrl =  Linuxx64Base,
                    Linuxarm64BaseUrl =  Linuxarm64Base,
                    ZnxUrl = znxUrl,
                    ReleaseNotesURL = item["comments"]?.InnerText,
                    IsInstalled = Directory.Exists(rp),
                    IsZNXInstalled = Directory.Exists(Path.Combine("Runners", $"runtime-{title}"))
                });
            }
            RuntimeList.ItemsSource = list.OrderByDescending(x => x.Date).ToList();
            client.Dispose();

            VerTitle.Text = "Select a Runtime";
            PatchNotesText.Text = "";
            LoadingArea.IsVisible = false;
            DetailsArea.IsVisible = true;
        }
        catch (Exception ex)
        {
            PatchNotesText.Text = "Error loading feed: " + ex.Message;
            LoadingArea.IsVisible = false;
            DetailsArea.IsVisible = true;
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RuntimeList.SelectedItem is YYRuntimeMetadata selected)
        {
            //json stuff
            using var client = new HttpClient();
            string releasenotes = File.ReadAllText(Path.Combine(cachePath, $"release-notes-{selected.Version}.json"));
            JObject jrn = JObject.Parse(releasenotes);
            string raw = jrn["release_notes"][0].ToString();

            raw = Regex.Replace(raw, "<.*?>", string.Empty);
            raw = WebUtility.HtmlDecode(raw);
            raw = raw.Replace("\\n", Environment.NewLine);
            raw = raw.Replace("\\\"", "\"");
            raw = raw.Replace("\\t", "");
            VerTitle.Text = "Version " + selected.Version;
            DownloadBtn.IsEnabled = !selected.IsInstalled || !selected.IsZNXInstalled;
            string btnstr = String.Empty;
            if (selected.IsInstalled && selected.IsZNXInstalled)
                btnstr = "Installed";
            else if ((selected.IsInstalled && !selected.IsZNXInstalled) || (!selected.IsInstalled && selected.IsZNXInstalled))
                btnstr = "Repair";
            else
                btnstr = "Download";
            DownloadBtn.Content = btnstr;
            PatchNotesText.Text = raw.Trim();
        }
    }

    private async void OnDownloadClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DownloadBtn.IsEnabled = false;
        DownProgress.IsVisible = true;
        DownProgress.Value = 0;
        try
        {
            if (RuntimeList.SelectedItem is YYRuntimeMetadata selected)
            {
                YYRuntimeMetadata data = (RuntimeList.SelectedItem as YYRuntimeMetadata)!;
                string installPath = string.Empty;
                string baseName = string.Empty;
                string modName = string.Empty;
                string modURL = string.Empty;
                bool pre20232 = false;
                bool lts = false;
                bool lts2026 = false;
                if (data.WinBaseURL == "")
                    pre20232 = true;
                if (data.Version.Contains("2022.0"))
                    lts = true;
                if (data.Version.Contains("2026"))
                    lts2026 = true;
                if (lts)
                    baseName = data.BaseURL.Replace("http://", ""); //thanks lts
                else
                    baseName = data.BaseURL.Replace("https://", "");
                baseName = baseName.Split("/")[1];
                if (!pre20232)
                {
                    switch (MainWindow.platform)
                    {
                        case "windows":
                            modName = data.WinBaseURL.Replace("https://", "");
                            modURL = data.WinBaseURL;
                            break;
                        case "osx":
                            if (MainWindow.architecture == Architecture.X64)
                            {
                                modName = data.OSXx64BaseUrl.Replace("https://", "");
                                modURL = data.OSXx64BaseUrl;
                            }
                            else
                            {
                                modName = data.OSXarm64BaseUrl.Replace("https://", "");
                                modURL = data.OSXarm64BaseUrl;
                            }
                            break;
                        case "linux":
                            if (MainWindow.architecture == Architecture.X64)
                            {
                                modName = data.Linuxx64BaseUrl.Replace("https://", "");
                                modURL = data.Linuxx64BaseUrl;
                            }
                            else
                            {
                                modName = data.Linuxarm64BaseUrl.Replace("https://", "");
                                modURL = data.Linuxarm64BaseUrl;
                            }
                            break;
                    }
                    modName = modName.Split("/")[1];
                }

                var ini = new IniFile();
                ini.Load(Path.Combine("Data", "config.ini"));

                bool downloadToPD = ini["Config"]["UseProgramData"].ToBool();
                string gmName = "";
                if (downloadToPD)
                {
                    gmName = "GameMakerStudio2";
                    if (lts)
                        gmName += "-LTS";
                    if (lts2026)
                        gmName += "-LTS2026";
                }
                else
                {
                    gmName = "gm";
                    if (lts)
                        gmName += "lts";
                    else if (lts2026)
                        gmName += "lts2026";
                    else
                        gmName += "monthly";
                }


                //paths!
                switch (MainWindow.platform)
                {
                    case "windows":
                        installPath = Path.Combine("C:", "ProgramData", gmName, "Cache", "runtimes", "zarfa").Replace("zarfa", "");
                        break;
                    case "osx":
                        installPath = "/" + Path.Combine("Users", "Shared", gmName, "Cache", "runtimes", "zarfa").Replace("zarfa", "");
                        break;
                    case "linux":
                        installPath = Path.Combine("Runners", gmName, "zarfa").Replace("zarfa", "");
                        break;
                }
                
                if (!downloadToPD)
                    installPath = Path.Combine("Runners", gmName, "zarfa").Replace("zarfa", "");

                using var client = new HttpClient();
                if (!pre20232)
                {
                    //base
                    if (!data.IsInstalled)
                    {
                        await DownloadFileAsync(client, data.BaseURL, Path.Combine(cachePath, baseName), 0, 33);
                        await DownloadFileAsync(client, modURL, Path.Combine(cachePath, modName), 33, data.IsZNXInstalled ? 100 : 66);
                    }
                    if (!data.IsZNXInstalled)
                        await DownloadFileAsync(client, data.ZnxUrl, Path.Combine(cachePath, data.Version + ".7z"), 66, 100);
                    DownProgress.IsIndeterminate = true;
                    Directory.CreateDirectory(Path.Combine(cachePath, $"runtime-{data.Version}"));
                    Directory.CreateDirectory(Path.Combine(cachePath, $"{data.Version}"));
                    if (!data.IsInstalled)
                    {
                        await ExtractRuntime(Path.Combine(cachePath, baseName), Path.Combine(cachePath, $"runtime-{data.Version}"), YYMD5.CalculateZipPassword(baseName));
                        await ExtractRuntime(Path.Combine(cachePath, modName), Path.Combine(cachePath, $"runtime-{data.Version}"), YYMD5.CalculateZipPassword(modName));
                    }
                    if (!data.IsZNXInstalled)
                        await ExtractRuntime(Path.Combine(cachePath, $"{data.Version}.7z"), Path.Combine(cachePath, data.Version));
                }
                else
                {
                    if (!data.IsInstalled)
                        await DownloadFileAsync(client, data.BaseURL, Path.Combine(cachePath, baseName), 0, data.IsZNXInstalled ? 100 : 50);
                    if (!data.IsZNXInstalled)
                        await DownloadFileAsync(client, data.ZnxUrl, Path.Combine(cachePath, $"{data.Version}.7z"), 50, 100);
                    DownProgress.IsIndeterminate = true;
                    Directory.CreateDirectory(Path.Combine(cachePath, $"runtime-{data.Version}"));
                    Directory.CreateDirectory(Path.Combine(cachePath, $"{data.Version}"));
                    if (!data.IsInstalled)
                        await ExtractRuntime(Path.Combine(cachePath, baseName), Path.Combine(cachePath, $"runtime-{data.Version}"), YYMD5.CalculateZipPassword(baseName));
                    if (!data.IsZNXInstalled)
                        await ExtractRuntime(Path.Combine(cachePath, $"{data.Version}.7z"), Path.Combine(cachePath, $"{data.Version}"));
                }
                if (!data.IsInstalled)
                {
                    MainWindow.CopyDirectory(Path.Combine(cachePath, $"runtime-{data.Version}"), $"{installPath}runtime-{data.Version}", true);
                    File.Delete(Path.Combine(cachePath, baseName));
                    if (!pre20232)
                        File.Delete(Path.Combine(cachePath, modName));
                }
                if (!data.IsZNXInstalled)
                {
                    MainWindow.CopyDirectory(Path.Combine(cachePath, $"{data.Version}"), $"Runners/runtime-{data.Version}", true);
                    File.Delete(Path.Combine(cachePath, $"{data.Version}.7z"));
                }

                Directory.Delete(Path.Combine(cachePath, $"runtime-{data.Version}"), true);
                Directory.Delete(Path.Combine(cachePath, $"{data.Version}"), true);
                
                //file permission stuff for unix/linux systems
                if (MainWindow.platform == "osx" || MainWindow.platform == "linux")
                {
                    File.SetUnixFileMode(Path.Combine($"{installPath}runtime-{data.Version}", "bin", "assetcompiler", MainWindow.platform, MainWindow.architecture.ToString().ToLower(), "GMAssetCompiler"), 
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }

                selected.IsInstalled = true;
                DownProgress.IsIndeterminate = false;
                DownloadBtn.Content = "Installed";
                _window.PopulateRuntimes();
            }
        }
        catch (Exception ex)
        {
            PatchNotesText.Text = "Error during download: " + ex.Message;
        }
        finally
        {
            DownProgress.IsVisible = false;
        }
    }

    private async Task ExtractRuntime(string zipPath, string targetDir, string password = "")
    {
        var options = new ReaderOptions { Password = password, LookForHeader = true, LeaveStreamOpen = false };

        if (password == "")
        {
            using (var reader = SevenZipArchive.OpenArchive(zipPath, options))
            {
                foreach (var entry in reader.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(targetDir, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }
        else
        {
            using (var reader = ZipArchive.OpenArchive(zipPath, options))
            {
                foreach (var entry in reader.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(targetDir, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }
    }

    private async Task DownloadFileAsync(HttpClient client, string url, string path, int minProgress, int maxProgress)
    {
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(path, FileMode.Create);

        var buffer = new byte[8192];
        var totalRead = 0L;
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            totalRead += read;

            if (totalBytes != -1)
            {
                var fileProgress = (double)totalRead / totalBytes;
                var totalProgress = minProgress + (fileProgress * (maxProgress - minProgress));
                Dispatcher.UIThread.Post(() => DownProgress.Value = totalProgress);
            }
        }
    }

    private void GetRuntimes(object sender, SelectionChangedEventArgs e)
    {
        if (!init) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
        {
            string tabHeader = selectedTab.Header?.ToString() ?? "";

            switch (tabHeader)
            {
                case "Monthly": 
                    LoadRuntimes(0);
                    break;
                case "LTS":
                    LoadRuntimes(1);
                    break;
                case "LTS2026":
                    LoadRuntimes(2);
                    break;
                case "Beta":
                    LoadRuntimes(3);
                    break;
            }
        }
    }
}