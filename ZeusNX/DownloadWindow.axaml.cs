using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Transactions;

namespace ZeusNX;

public partial class DownloadWindow : Window
{
    public DownloadWindow(Action<string, string> traceAction)
    {
        InitializeComponent();
        var trace = traceAction;
    }
}