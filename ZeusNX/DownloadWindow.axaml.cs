using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;

namespace ZeusNX;
public class YYRuntimeMetadata
{
    public string Version { get; set; }
    public string Date { get; set; }
    public string ReleaseNotesURL { get; set; }
    public string BaseURL { get; set; }
    public string WinBaseURL { get; set; }
    public bool IsInstalled { get; set; }
}

public partial class DownloadWindow : Window
{
    string cachePath = "Data\\Cache\\";
    public DownloadWindow(Action<string, string> traceAction)
    {
        InitializeComponent();
        var trace = traceAction;
        LoadRuntimes();
    }

    private async void LoadRuntimes()
    {
        LoadingArea.IsVisible = true;
        DetailsArea.IsVisible = false;
        try
        {
            using var client = new HttpClient();
            byte[] byteStream = await client.GetByteArrayAsync("https://gms.yoyogames.com/Zeus-Runtime.rss");
            File.WriteAllBytesAsync($"{cachePath}Zeus-Runtime.rss", byteStream);
            byteStream = await client.GetByteArrayAsync("https://gms.yoyogames.com/Zeus-Runtime-LTS.rss");
            File.WriteAllBytesAsync($"{cachePath}Zeus-Runtime-LTS.rss", byteStream);

            string xml = File.ReadAllText($"{cachePath}Zeus-Runtime.rss");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            // Handle Namespaces
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("sparkle", "http://www.andymatuschak.org/xml-namespaces/sparkle");

            var list = new List<YYRuntimeMetadata>();
            var items = doc.GetElementsByTagName("item");

            foreach (XmlNode item in items)
            {
                //right depending on the runtime the base-module stuff doesn't exist, basically anything pre 2023.2
                string title = item["title"]?.InnerText.Replace("Version ", "");
                //if it's below 2022, don't even bother rn.+
                if (title.Split('.')[0] == "2") continue;
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
                    ReleaseNotesURL = item["comments"]?.InnerText
                });
            }
            RuntimeList.ItemsSource = list.OrderByDescending(x => x.Date).ToList();
            client.Dispose();
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

            VerTitle.Text = "Version " + selected.Version;
            DownloadBtn.IsEnabled = true;
            PatchNotesText.Text = jrn["release_notes"].ToString();
        }
    }

    private async void OnDownloadClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

    }
}