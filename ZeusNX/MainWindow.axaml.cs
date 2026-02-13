using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZeusNX
{
    public partial class MainWindow : Window
    {

        public string compilerPath = "\\bin\\assetcompiler\\windows\\x64"; //append to runtime path.

        public MainWindow()
        {
            InitializeComponent();
            PopulateRuntimes();
            trace("INFO", "Welcome to ZeusNX, Version 0.0.0");
        }

        public void trace(string type, string message)
        {
            logbox.Text += $"[{type}]: {message}\n";
        }

        private void OnRefreshRuntimesClicked(object sender, RoutedEventArgs e)
        {
            PopulateRuntimes();
            trace("INFO", "Runtime list refreshed.");
        }

        private async void OnBrowseProjectClicked(object sender, RoutedEventArgs e)
        {
            var toplevel = TopLevel.GetTopLevel(this);
            var file = await toplevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select GMS2 Project",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("GMS2 Project")
                    {
                        Patterns = new List<string> { "*.yyp" }
                    }
                },
                AllowMultiple = false
            });

            if (file.Count > 0)
            {
                projpath.Text = file[0].Path.LocalPath;
                trace("INFO", $"Selected project: {projpath.Text}");
            }
        }

        private void OnTitleIdInput(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string currentText = textBox.Text ?? "";
                string newText = Regex.Replace(currentText.ToUpper(), "[^0-9A-F]", "");

                if (textBox.Text != newText)
                {
                    int selectionStart = textBox.CaretIndex;
                    textBox.Text = newText;
                    textBox.CaretIndex = selectionStart;
                }
            }
        }

        private async void OnSelectIconClicked(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select JPG (256x256)",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("JPG Image")
                    {
                        Patterns = new List<string> { "*.jpg", "*.jpeg" }
                    }
                },
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                string filePath = files[0].Path.LocalPath;
                if (!filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    trace("ERROR", "File must be a JPG!");
                    return;
                }

                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var bitmap = new Bitmap(stream);
                        if (bitmap.PixelSize.Width == 256 && bitmap.PixelSize.Height == 256)
                        {
                            gameico.Source = bitmap;
                            trace("INFO", $"Icon loaded: {filePath}");
                        }
                        else
                        {
                            trace("ERROR", $"Image size is {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}. Must be 256x256!");
                            bitmap.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to load image: {ex.Message}");
                }
            }
        }

        private async void OnBrowseKeysClicked(object sender, RoutedEventArgs e)
        {
            var toplevel = TopLevel.GetTopLevel(this);
            var file = await toplevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select prod.Keys",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Switch Keys")
                    {
                        Patterns = new List<string> { "*.keys" }
                    }
                },
                AllowMultiple = false
            });

            if (file.Count > 0)
            {
                keypath.Text = file[0].Path.LocalPath;
                trace("INFO", $"Selected keys: {keypath.Text}");
            }
        }

        private void PopulateRuntimes()
        {
            var runtimes = new List<string>();
            try
            {
                //mainline
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Mainline").ToList());
                else
                    trace("ERROR",  "No Mainline Runtimes Found!");

                //LTS
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-LTS\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-LTS\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | LTS").ToList());
                else
                    trace("WARN",  "No LTS Runtimes Found!");

                //Beta
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-Beta\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-Beta\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Beta").ToList());
                else
                    trace("WARN",  "No Beta Runtimes Found!");

                //Nocturnus
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-Dev\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-Dev\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Dev").ToList());
                else
                    trace("WARN", "No Dev Runtimes Found!");

                runtimesel.ItemsSource = runtimes;
            }
            catch (Exception ex)
            {
                trace("ERROR",  ex.Message);
            }
        }

        private void OnCleanClicked(object sender, RoutedEventArgs e)
        {
            logbox.Text = "";
        }

        public async void BuildNSP(object sender, RoutedEventArgs e)
        {
            buildnsp.IsEnabled = false;
            //TODO uhhh add detection for pre 2.3 projects and pre 2024 projects, formats for the options_switch.yy is different
            //support latest mainline release (2024.14.3.260) and latest lts (2022.0.3.99) on release, MAYBE beta for that one undertale thing. leave nocturnus alone since that's internal yoyogames shit
            trace("INFO", "Build START!");
            var cracked = false; //if gmassetcompiler is cracked or not, should be for me only.
            //start by checking if shit is filled out
            var projPath = projpath.Text;
            var titleID = titleid.Text;
            var titleName = titlename.Text == null ? "ZeusNX Application" : titlename.Text;
            var titleAuthor = titleauthor.Text == null ? "ZeusNX User" : titleauthor.Text;
            var titleVer = titleversion.Text == null ? "0.0.0" : titleversion.Text;
            var projConfig = projconf.Text == null ? "Default" : projconf.Text;
            var keyPath = keypath.Text;

            if (keyPath == null || !keyPath.Contains(".keys"))
            {
                trace("ERROR", "Keys not found!!!");
                return;
            }
            if (projPath == null || !projPath.Contains(".yyp"))
            {
                trace("ERROR", "Invalid project file!");
                return;
            }
            if (titleID == null || titleID.Length < 16)
            {
                trace("ERROR", "Invalid TitleID!");
                return;
            }
            var selectedRuntime = runtimesel.SelectedItem as string;
            var branch = selectedRuntime?.Split('|')[1].Trim();
            selectedRuntime = selectedRuntime?.Split('|')[0].Trim();
            selectedRuntime = $"runtime-{selectedRuntime}";
            var runtimePath = $"C:\\ProgramData\\GameMakerStudio2{(branch == "Mainline" ? "" : $"-{branch}")}\\Cache\\runtimes\\{selectedRuntime}";

            if (!Directory.Exists(runtimePath))
            {
                trace("ERROR", $"{selectedRuntime} not found. do you have the runtime installed?");
                return;
            }
            //check if associated ZeusNX runtime is here, otherwise halt you kinda need those to make a build
            if (!Directory.Exists($"Runners\\{selectedRuntime}"))
            {
                trace("ERROR", $"{selectedRuntime} files not found! Either follow the guide or check the Github repo to make sure you have it.");
                return;
            }

            //patch GMAssetCompiler.dll to ignore licence checks
            trace("INFO", "Patching GMAssetCompiler.dll...");
            File.Copy($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll", $"{runtimePath}{compilerPath}\\GMAssetCompiler.bak");
            File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
            await Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = $"Tools\\xdelta.exe",
                    Arguments = $"-d -s \"{runtimePath}{compilerPath}\\GMAssetCompiler.bak\" \"Runners\\patches\\{selectedRuntime}.xdelta\" \"{runtimePath}{compilerPath}\\GMAssetCompiler.dll\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.UIThread.InvokeAsync(() => trace("XDELTA", e.Data));
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.UIThread.InvokeAsync(() => trace("XDELTAERR", e.Data));
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        trace("ERROR", $"XDelta exited with code {process.ExitCode}, Something went wrong.");
                        return;
                    }      
                }
            });

            //make temp directories and everything
            trace("INFO", "Creating build dir...");
            if (!Directory.Exists($"{titleName}_build") || !Directory.EnumerateFileSystemEntries($"{titleName}_build").Any())
            {
                Directory.CreateDirectory($"{titleName}_build");
                Directory.CreateDirectory($"{titleName}_build\\tmp");
                Directory.CreateDirectory($"{titleName}_build\\cache");
                Directory.CreateDirectory($"{titleName}_build\\nsp");
                Directory.CreateDirectory($"{titleName}_build\\nsp\\exefs");
                Directory.CreateDirectory($"{titleName}_build\\nsp\\romfs");
                Directory.CreateDirectory($"{titleName}_build\\nsp\\control");
                Directory.CreateDirectory($"{titleName}_build\\nsp\\logo");
            }

            trace("INFO", "Preprocessing GMS2 project...");
            string AssetCompilerARG = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{titleName}\" /td=\"{titleName}_build\\tmp\" /cd=\"{titleName}_build\\cache\" /rtp=\"{runtimePath}\" /ffe=\"eXpvfGtxgjeDg202c3h+b3Z2c31veH1vNnh/dnZzfXI2dnlxc3hpfX15Nn5vfX4=\" /m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /sh=True /nodnd /cfg=\"{projConfig}\" /o=\"{titleName}_build\\nsp\\romfs\" /optionsini=\"C:\\Users\\amyme\\Documents\\RussellNX\\runners\\build2024.14.3.260\\romfs\\options.ini\" /baseproject=\"\" \"{projPath}\" /v /preprocess=\"{titleName}_build\\cache\"";

            trace("INFO", $"GMAC ARGS: {AssetCompilerARG}");
            await Task.Run(() =>
            {
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = $"{runtimePath}{compilerPath}\\GMAssetCompiler.exe",
                        Arguments = AssetCompilerARG,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.OutputDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Dispatcher.UIThread.InvokeAsync(() => trace("GMAC", e.Data));
                        };
                        process.ErrorDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Dispatcher.UIThread.InvokeAsync(() => trace("GMACERR", e.Data));
                        };
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            trace("ERROR", $"GMAssetCompiler exited with code {process.ExitCode}, Something went wrong.");
                    }
                }
            });
            trace("INFO", "Compiling GMS2 project...");
            AssetCompilerARG = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{titleName}\" /td=\"{titleName}_build\\tmp\" /cd=\"{titleName}_build\\cache\" /rtp=\"{runtimePath}\" /ffe=\"eXpvfGtxgjeDg202c3h+b3Z2c31veH1vNnh/dnZzfXI2dnlxc3hpfX15Nn5vfX4=\" /m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /sh=True /nodnd /cfg=\"{projConfig}\" /o=\"{titleName}_build\\nsp\\romfs\" /optionsini=\"C:\\Users\\amyme\\Documents\\RussellNX\\runners\\build2024.14.3.260\\romfs\\options.ini\" /baseproject=\"\" \"{projPath}\" /v";
            await Task.Run(() =>
            {
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = $"{runtimePath}{compilerPath}\\GMAssetCompiler.exe",
                        Arguments = AssetCompilerARG,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.OutputDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Dispatcher.UIThread.InvokeAsync(() => trace("GMAC", e.Data));
                        };
                        process.ErrorDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Dispatcher.UIThread.InvokeAsync(() => trace("GMACERR", e.Data));
                        };
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            trace("ERROR", $"GMAssetCompiler exited with code {process.ExitCode}, Something went wrong.");
                    }
                }
            });

            //cleanup
            trace("INFO", "Restoring GMAssetCompiler.dll");
            File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
            File.Copy($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak", $"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
            File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak");
            Directory.Delete($"{titleName}_build\\tmp", true);
            Directory.Delete($"{titleName}_build\\cache", true);
            //Directory.Delete($"{titleName}_build\\nsp", true);
            trace("INFO", "Build Complete!");
            buildnsp.IsEnabled = true;
        }
    }
}