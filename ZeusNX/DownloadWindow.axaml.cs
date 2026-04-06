using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Transactions;
using ZeusNX.Metadata;

namespace ZeusNX;

public partial class DownloadWindow : Window
{
    public DownloadWindow(Action<string, string> traceAction)
    {
        InitializeComponent();
        var trace = traceAction;
    }

    private async void LoadRuntimes()
    {
        var list = new List<YYRuntimeMetadata>();
        using var client = new HttpClient();
        var xml = await client.GetStringAsync("https://gms.yoyogames.com/Zeus-Runtime.rss");
    }
}