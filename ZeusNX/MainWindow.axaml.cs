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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZeusNX.Ini;
using ZeusNX.YYOptions;

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

        //thank you https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
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

        private async void OnSelectSplashClicked(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select PNG",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("PNG Image")
                    {
                        Patterns = new List<string> { "*.png" }
                    }
                },
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                string filePath = files[0].Path.LocalPath;
                if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    trace("ERROR", "File must be a PNG!");
                    return;
                }

                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var bitmap = new Bitmap(stream);
                        gamesplash.Source = bitmap;
                        trace("INFO", $"Splash loaded: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to load image: {ex.Message}");
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

            //we should check project compatibility here since running lts on a 2024 runtime will make it crash the fuck out
            //google "how to get version from yyp which is just an evil json"
            string YYP = File.ReadAllText(projPath);
            JsonDocument jYYP = JsonDocument.Parse(YYP, new JsonDocumentOptions { AllowTrailingCommas = true});
            JsonElement root = jYYP.RootElement;
            if (root.TryGetProperty("MetaData", out JsonElement nameElement))
            {
                if (nameElement.TryGetProperty("IDEVersion", out JsonElement IDEVer))
                {
                    string versionString = IDEVer.ToString();
                    string temp = versionString.Split('.')[0];
                    temp += "." + versionString.Split('.')[1];
                    string temp2 = selectedRuntime.Split(".")[0];
                    temp2 = "." + selectedRuntime.Split(".")[1];
                    temp2.Replace("runtime-", "");
                    if (temp != temp2)
                    {
                        trace("WARN", "Project versions don't match, there may be dragons!");
                    }
                }
            }


            //patch GMAssetCompiler.dll to ignore licence checks
            trace("INFO", "Patching GMAssetCompiler.dll...");
            if (File.Exists($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak"))
            {
                File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
                File.Copy($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak", $"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
                File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak");
            }
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
            var time = DateTime.Now.ToString();
            time = time.Replace(" ", "");
            time = time.Replace(":", ".");
            time = time.Replace("-", ".");
            var buildDir = $"{titleName}_build{time}";
            if (!Directory.Exists(buildDir) || !Directory.EnumerateFileSystemEntries(buildDir).Any())
            {
                Directory.CreateDirectory(buildDir);
                Directory.CreateDirectory($"{buildDir}\\tmp");
                Directory.CreateDirectory($"{buildDir}\\cache");
                Directory.CreateDirectory($"{buildDir}\\nsp");
                Directory.CreateDirectory($"{buildDir}\\nsp\\exefs");
                Directory.CreateDirectory($"{buildDir}\\nsp\\romfs");
                Directory.CreateDirectory($"{buildDir}\\nsp\\control");
                Directory.CreateDirectory($"{buildDir}\\nsp\\logo");
            }
            else
            {
                trace("ERROR", $"{buildDir} has data inside!!");
                return;
            }
                //copy runtime files to exefs / control
            trace("INFO", "Copying runtime files...");
            CopyDirectory($"Runners\\{selectedRuntime}\\bin", $"{buildDir}\\nsp\\exefs", true);
            CopyDirectory($"Runners\\shared\\logo", $"{buildDir}\\nsp\\logo", true);
            //CopyDirectory($"Runners\\{selectedRuntime}\\romfs", $"{buildDir}\\nsp\\romfs", true);

            //generate options.ini out of thin air
            var optionsINI = new IniFile();
            optionsINI["LLVM-Switch"]["SDKDir"] = "C:\\Nintendo\\NXSDK\\NintendoSDK";
            optionsINI["LLVM-Switch"]["UseNEX"] = false;
            optionsINI["LLVM-Switch"]["UseNPLN"] = false;
            optionsINI["LLVM-Switch"]["nMeta"] = "C:\\Users\\ZeusNX\\Project\\options\\switch\\application.nmeta";
            optionsINI.Save($"{buildDir}\\nsp\\romfs");

            //make the preselecteduser file
            File.Create($"{buildDir}\\nsp\\romfs\\preselecteduser");
            if (preselecteduserCheck.IsChecked == true)
                File.WriteAllText($"{buildDir}\\nsp\\romfs\\preselecteduser", "False");
            else
                File.WriteAllText($"{buildDir}\\nsp\\romfs\\preselecteduser", "True");


            trace("INFO", "Preprocessing GMS2 project...");
            string AssetCompilerARG = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{titleName}\" /td=\"{buildDir}\\tmp\" /cd=\"{buildDir}\\cache\" /rtp=\"{runtimePath}\" /ffe=\"eXpvfGtxgjeDg202c3h+b3Z2c31veH1vNnh/dnZzfXI2dnlxc3hpfX15Nn5vfX4=\" /m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /sh=True /nodnd /cfg=\"{projConfig}\" /o=\"{buildDir}\\nsp\\romfs\" /optionsini=\"C:\\Users\\amyme\\Documents\\RussellNX\\runners\\build2024.14.3.260\\romfs\\options.ini\" /baseproject=\"\" \"{projPath}\" /v /preprocess=\"{buildDir}\\cache\"";

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
            AssetCompilerARG = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{titleName}\" /td=\"{buildDir}\tmp\" /cd=\"{buildDir}\\cache\" /rtp=\"{runtimePath}\" /ffe=\"eXpvfGtxgjeDg202c3h+b3Z2c31veH1vNnh/dnZzfXI2dnlxc3hpfX15Nn5vfX4=\" /m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /sh=True /nodnd /cfg=\"{projConfig}\" /o=\"{buildDir}\\nsp\\romfs\" /optionsini=\"\" /baseproject=\"\" \"{projPath}\" /v";
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
            
            //now starts the fun part, copy over selected icon
            //TODO: add language support
            if (gameico.Source != null)
            {
                trace("INFO", "Copying over icon...");
                var bitmap = gameico.Source as Bitmap;
                using (var stream = File.OpenWrite($"{buildDir}\\nsp\\control\\icon_AmericanEnglish.dat"))
                {
                    bitmap.Save(stream);
                }
            }

            //copy over splash if toggle is set
            if (splashCheck.IsChecked == true && gamesplash.Source != null)
            {
                trace("INFO", "Copying over splash...");
                var bitmap = gamesplash.Source as Bitmap;
                using (var stream = File.OpenWrite($"{buildDir}\\nsp\\romfs\\splash.png"))
                {
                    bitmap.Save(stream);
                }
            }

            //cleanup
            trace("INFO", "Restoring GMAssetCompiler.dll");
            File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
            File.Copy($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak", $"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
            File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak");
            if (Directory.Exists($"{buildDir}\\tmp"))
                Directory.Delete($"{buildDir}\\tmp", true);
            if (Directory.Exists($"{buildDir}\\cache"))
                Directory.Delete($"{buildDir}\\cache", true);
            //if (Directory.Exists($"{buildDir}\\nsp"))
                //Directory.Delete($"{buildDir}\\nsp", true);
            trace("INFO", "Build Complete!");
            buildnsp.IsEnabled = true;
        }
    }
}