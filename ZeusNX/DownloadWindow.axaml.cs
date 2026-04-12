using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;
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
    public bool IsInstalled { get; set; }

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
    public bool IsInstalled { get; set; }

    public string DisplayVersion => Name;
    public string DisplaySubtitle => Date;
}

public partial class DownloadWindow : Window
{
    string cachePath = "Data\\Cache\\";
    bool init = false;
    public DownloadWindow()
    {
        InitializeComponent();
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

            switch (runtype)
            {
                case 0:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime.rss");
                    break;
                case 1:
                    xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime-LTS.rss");
                    break;
                case 2:
                    xml = await client.GetStringAsync("https://sorastream.dev/zeusnx/files.json"); //lol this is not xml brah
                    break;
            }

            //official yyg stuff
            if (runtype < 2)
            {

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("sparkle", "http://www.andymatuschak.org/xml-namespaces/sparkle");

                var items = doc.GetElementsByTagName("item");

                foreach (XmlNode item in items)
                {
                    //right depending on the runtime the base-module stuff doesn't exist, basically anything pre 2023.2. lts is infact, pre 2023.2.
                    string title = item["title"]?.InnerText.Replace("Version ", "");
                    //if it's below 2022, don't even bother rn.
                    if (title.Split('.')[0] == "2") continue;
                    //blacklist 2022.3 and lower since that uses an older build system, and i haven't researched how to patch that yet
                    if (Double.Parse($"{title.Split('.')[0]}.{title.Split('.')[1]}") <= 2022.4 && Double.Parse($"{title.Split('.')[0]}.{title.Split('.')[1]}") != 2022.11 && runtype == 0) continue;
                    var enclosure = item.SelectSingleNode("enclosure");
                    if (enclosure == null) continue;
                    var WinBase = enclosure.Attributes["url"]?.Value;
                    if (Int32.Parse(title.Split('.')[0]) >= 2023)
                    {
                        if (!title.Contains("2023.1"))
                            WinBase = enclosure.SelectSingleNode("module[@name='base-module-windows-x64']").Attributes["url"]?.Value;
                    }
                    string rawDate = item["pubDate"]?.InnerText ?? "";
                    string cleanDate = DateTime.TryParse(rawDate, out var dt) ? dt.ToShortDateString() : rawDate;
                    if (!File.Exists($"{cachePath}release-notes-{title}.json"))
                    {
                        byteStream = await client.GetByteArrayAsync(item["comments"]?.InnerText ?? "");
                        File.WriteAllBytesAsync($"{cachePath}release-notes-{title}.json", byteStream);
                    }
                    list.Add(new YYRuntimeMetadata
                    {
                        Version = item["title"]?.InnerText.Replace("Version ", ""),
                        Date = cleanDate,
                        BaseURL = enclosure.Attributes["url"]?.Value,
                        WinBaseURL = WinBase,
                        ReleaseNotesURL = item["comments"]?.InnerText,
                        IsInstalled = Directory.Exists($"C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes\\runtime-{title}")
                    });
                }
                RuntimeList.ItemsSource = list.OrderByDescending(x => x.Date).ToList();
                client.Dispose();
            }
            else
            {
                List<ZNXRuntimeMetadata> feed = JsonConvert.DeserializeObject<List<ZNXRuntimeMetadata>>(xml); //not xml here. maybe i should. change that var name.
                foreach (var item in feed)
                {
                    item.IsInstalled = Directory.Exists($"Runners\\runtime-{item.Name}");
                }
                RuntimeList.ItemsSource = feed.OrderByDescending(x => x.Date);
            }

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
            string releasenotes = File.ReadAllText($"{cachePath}\\release-notes-{selected.Version}.json");
            JObject jrn = JObject.Parse(releasenotes);
            client.Dispose();
            string raw = jrn["release_notes"][0].ToString();

            raw = Regex.Replace(raw, "<.*?>", string.Empty);
            raw = WebUtility.HtmlDecode(raw);
            raw = raw.Replace("\\n", Environment.NewLine);
            raw = raw.Replace("\\\"", "\"");
            raw = raw.Replace("\\t", "");
            VerTitle.Text = "Version " + selected.Version;
            DownloadBtn.IsEnabled = !selected.IsInstalled;
            DownloadBtn.Content = !selected.IsInstalled ? "Download" : "Installed";
            PatchNotesText.Text = raw.Trim();
        }
        else if (RuntimeList.SelectedItem is ZNXRuntimeMetadata znxselected)
        {
            VerTitle.Text = "Version " + znxselected.Name;
            DownloadBtn.IsEnabled = !znxselected.IsInstalled;
            DownloadBtn.Content = !znxselected.IsInstalled ? "Download" : "Installed";
            PatchNotesText.Text = znxselected.Note;
        }
    }

    private async void OnDownloadClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DownloadBtn.IsEnabled = false;
        DownProgress.IsVisible = true;
        DownProgress.Value = 0;
        try
        {
            if (RuntimeList.SelectedItem is ZNXRuntimeMetadata znx)
            {
                string installPath = "Runners\\";
                string uri = "https://sorastream.dev/zeusnx/";
                string zipName = znx.File.Replace(".7z", "");
                var client = new HttpClient();

                await DownloadFileAsync(client, $"{uri}{znx.File}", $"{cachePath}{znx.File}", 0, 100);
                DownProgress.IsIndeterminate = true;
                Directory.CreateDirectory($"{cachePath}znx{zipName}");
                await ExtractRuntime($"{cachePath}{znx.File}", $"{cachePath}znx{zipName}");
                client.Dispose();
                MainWindow.CopyDirectory($"{cachePath}znx{zipName}", $"{installPath}{zipName}", true);
                File.Delete($"{cachePath}{znx.File}");
                Directory.Delete($"{cachePath}znx{zipName}", true);

                znx.IsInstalled = true;
                DownProgress.IsIndeterminate = false;
                DownloadBtn.Content = "Installed";
            }
            else if (RuntimeList.SelectedItem is YYRuntimeMetadata selected)
            {
                YYRuntimeMetadata data = (RuntimeList.SelectedItem as YYRuntimeMetadata)!;
                string installPath = $"C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes\\";
                string baseName = string.Empty;
                string winmodName = string.Empty;
                bool pre20232 = false;
                bool lts = false;
                if (data.BaseURL == data.WinBaseURL)
                    pre20232 = true;
                if (data.Version.Contains("2022.0"))
                    lts = true;
                if (lts)
                    baseName = data.BaseURL.Replace("http://", ""); //thanks lts
                else
                    baseName = data.BaseURL.Replace("https://", "");
                baseName = baseName.Split("/")[1];
                if (!pre20232)
                {
                    winmodName = data.WinBaseURL.Replace("https://", "");
                    winmodName = winmodName.Split("/")[1];
                }
                using var client = new HttpClient();
                if (!pre20232)
                {
                    //base
                    await DownloadFileAsync(client, data.BaseURL, $"{cachePath}{baseName}", 0, 50);
                    await DownloadFileAsync(client, data.WinBaseURL, $"{cachePath}{winmodName}", 50, 100);
                    DownProgress.IsIndeterminate = true;
                    Directory.CreateDirectory($"{cachePath}runtime-{data.Version}");
                    await ExtractRuntime($"{cachePath}{baseName}", $"{cachePath}runtime-{data.Version}", YYMD5.CalculateZipPassword(baseName));
                    await ExtractRuntime($"{cachePath}{winmodName}", $"{cachePath}runtime-{data.Version}", YYMD5.CalculateZipPassword(winmodName));
                }
                else
                {
                    await DownloadFileAsync(client, data.BaseURL, $"{cachePath}{baseName}", 0, 100);
                    DownProgress.IsIndeterminate = true;
                    Directory.CreateDirectory($"{cachePath}runtime-{data.Version}");
                    await ExtractRuntime($"{cachePath}{baseName}", $"{cachePath}runtime-{data.Version}", YYMD5.CalculateZipPassword(baseName));
                }
                client.Dispose();
                MainWindow.CopyDirectory($"{cachePath}runtime-{data.Version}", $"{installPath}runtime-{data.Version}", true);
                File.Delete($"{cachePath}{baseName}");
                if (!pre20232)
                    File.Delete($"{cachePath}{winmodName}");
                Directory.Delete($"{cachePath}runtime-{data.Version}", true);
                selected.IsInstalled = true;
                DownProgress.IsIndeterminate = false;
                DownloadBtn.Content = "Installed";
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
                case "ZeusNX":
                    LoadRuntimes(2);
                    break;
            }
        }
    }
}