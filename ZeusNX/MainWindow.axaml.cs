using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ZeusNX.Ini;
using ZeusNX.Metadata;
using ZeusNX.NMeta;
using ZeusNX.YYOptions;

namespace ZeusNX
{
    public partial class MainWindow : Window
    {
        private string ZeusNXVersion = "1.0.1β3";
        private int langIndex = 0;
        public static string platform = "windows";
        public static Architecture architecture = RuntimeInformation.OSArchitecture; //also figure this out later, there will only be arm64 and x64 builds
        public bool enablePrefab = false;
        public string compilerPath = string.Empty;

        public string linuxBasePath = string.Empty;
        public List<string> languages = new List<string> { "AmericanEnglish",
                                                           "CanadianFrench",
                                                           "LatinAmericanSpanish",
                                                           "BrazilianPortuguese",
                                                           "Japanese",
                                                           "SimplifiedChinese",
                                                           "TraditionalChinese",
                                                           "Korean",
                                                           "BritishEnglish",
                                                           "French",
                                                           "German",
                                                           "Spanish",
                                                           "Italian",
                                                           "Dutch",
                                                           "Portuguese",
                                                           "Russian"};

        public string splashPath = Path.Combine("Runners", "shared", "splash_default.png");
        public Dictionary<string, string> langNames = new Dictionary<string, string>();
        public Dictionary<string, string> icoPaths = new Dictionary<string, string>();
        public Dictionary<string, string> titleNames = new Dictionary<string, string>();
        public Dictionary<string, string> titleAuthors = new Dictionary<string, string>();
        private readonly BlockingCollection<string> logQueue = new BlockingCollection<string>();
        private readonly CancellationTokenSource logCts = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            Task.Run(ProcessLogQueue);
            InitDict();
            initConfig();
            loadConfig();
            PopulateRuntimes();
            PopulateMetadata();
            if (metalist.SelectedItem != null)
                LoadMetadata(null, null);
            trace("INFO", $"Welcome to ZeusNX, Version {ZeusNXVersion} ({platform}, {architecture.ToString().ToLower()})");
            trace("DEBUG", $"Asset Compiler Path Is: {compilerPath}");
            if (platform == "linux")
                linuxBasePath = $"/{AppContext.BaseDirectory.Split('/')[1]}/{AppContext.BaseDirectory.Split('/')[2]}/";
        }
        protected override void OnClosed(EventArgs e)
        {
            logCts.Cancel();
            logQueue.Dispose();
            base.OnClosed(e);
        }

        //thank you https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        static public void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
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
            if (message == null || message == "" || message == string.Empty) return;
#if RELEASE
            if (type == "DEBUG") return;
#endif
            logQueue.Add($"[{type}]: {message}\n");
            //try
            //{
            //    logbox.Text += $"[{type}]: {message}\n";
            //    logbox.CaretIndex = logbox.Text.Length;
            //    logbox.SelectionStart = logbox.Text.Length;
            //    logbox.SelectionEnd = logbox.Text.Length;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Failed to update logbox: {ex.Message}");
            //}

        }
        private async Task ProcessLogQueue()
        {
            var stringBuilder = new StringBuilder();

            try
            {
                foreach (var logEntry in logQueue.GetConsumingEnumerable(logCts.Token))
                {
                    stringBuilder.Append(logEntry);
                    while (logQueue.TryTake(out var extraLog))
                        stringBuilder.Append(extraLog);
                    string chunkToLog = stringBuilder.ToString();
                    stringBuilder.Clear();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            logbox.Text += chunkToLog;
                            logbox.CaretIndex = logbox.Text.Length;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to update logbox: {ex.Message}");
                        }
                    }, DispatcherPriority.Background);
                    await Task.Delay(10, logCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void PopulateMetadata()
        {
            metalist.ItemsSource = Directory.GetFiles(Path.Combine("Data", "Metadata")).Select(f => new FileInfo(f)).OrderByDescending(f => f.LastAccessTime).Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList();
        }

        private void InitDict()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                platform = "windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                platform = "osx";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                platform = "linux";
            
            switch (platform)
            {
                case "windows":
                    compilerPath = Path.Combine("bin", "assetcompiler", "windows", "x64");
                    break;
                case "osx":
                    compilerPath = Path.Combine("bin", "assetcompiler", "osx", architecture.ToString().ToLower());
                    break;
                case "linux":
                    compilerPath = Path.Combine("bin", "assetcompiler", "linux", architecture.ToString().ToLower());
                    break;
            }
            
            Directory.CreateDirectory("Runners");
            Directory.CreateDirectory(Path.Combine("Runners", "gmmonthly"));
            Directory.CreateDirectory(Path.Combine("Runners", "gmlts"));
            Directory.CreateDirectory(Path.Combine("Runners", "gmlts2026"));
            Directory.CreateDirectory(Path.Combine("Runners", "gmbeta"));
            Directory.CreateDirectory("Data");
            Directory.CreateDirectory(Path.Combine("Data", "Metadata"));
            Directory.CreateDirectory(Path.Combine("Data", "Icons"));
            Directory.CreateDirectory(Path.Combine("Data", "Splashes"));
            Directory.CreateDirectory(Path.Combine("Data", "Cache"));
            //texture page stuff
            List<string> txtPageList = new List<string> { "256x256", "512x512", "1024x1024", "2048x2048", "4096x4096", "8192x8192", "16384x16384" };
            //lang stuff
            langNames.Add("AmericanEnglish", "American English");
            langNames.Add("CanadianFrench", "Canadian French");
            langNames.Add("LatinAmericanSpanish", "Latin American Spanish");
            langNames.Add("BrazilianPortuguese", "Brazilian Portuguese");
            langNames.Add("Japanese", "Japanese");
            langNames.Add("SimplifiedChinese", "Chinese (Simplified)");
            langNames.Add("TraditionalChinese", "Chinese (Traditional)");
            langNames.Add("Korean", "Korean");
            langNames.Add("BritishEnglish", "British English");
            langNames.Add("French", "French");
            langNames.Add("German", "German");
            langNames.Add("Spanish", "European Spanish");
            langNames.Add("Italian", "Italian");
            langNames.Add("Dutch", "Dutch");
            langNames.Add("Portuguese", "Portuguese");
            langNames.Add("Russian", "Russian");
            
            foreach (var lang in languages)
            {
                icoPaths.Add(lang, Path.Combine("Runners", "shared", "ico_default.jpg"));
                titleNames.Add(lang, "ZeusNX Application");
                titleAuthors.Add(lang, "ZeusNX User");
            }

            //init everything using first lang
            currentLang.Text = langNames["AmericanEnglish"];
            gameico.Source = new Bitmap(icoPaths["AmericanEnglish"]);
            gamesplash.Source = new Bitmap(Path.Combine("Runners", "shared", "splash_default.png"));
            titlename.Text = titleNames["AmericanEnglish"];
            titleauthor.Text = titleAuthors["AmericanEnglish"];
            texturesizesel.ItemsSource = txtPageList;
            texturesizesel.SelectedIndex = 3;
        }

        private void initConfig()
        {
            if (platform == "linux")
                cbUseProgramData.IsVisible = false;

            if (File.Exists("Data/prod.keys"))
            {
                btnInstallKeys.Content = "Keys installed";
                btnInstallKeys.IsEnabled = false;
            }
            
            if (File.Exists(Path.Combine("Data", "config.ini"))) return;
            
            var ini = new IniFile();
            ini["Config"]["UseProgramData"] = false;
            ini["Config"]["UseGMCache"] = false;
            ini.Save(Path.Combine("Data", "config.ini"));
        }

        private void loadConfig()
        {
            var ini = new IniFile();
            ini.Load(Path.Combine("Data", "config.ini"));
            cbUseProgramData.IsChecked = ini["Config"]["UseProgramData"].ToBool();
            cbUseGMCache.IsChecked = ini["Config"]["UseGMCache"].ToBool();
        }

        private void OnOpenDownloaderClicked(object sender, RoutedEventArgs e)
        {
            var downloadWin = new DownloadWindow(this);
            downloadWin.ShowDialog(this);
        }

        private void OnNextLangClicked(object sender, RoutedEventArgs e)
        {
            //save everything (icon is saved on select)
            saveLang();
            langIndex = (langIndex + 1) % languages.Count;
            updateUI();
        }

        private void OnPrevLangClicked(object sender, RoutedEventArgs e)
        {
            saveLang();
            langIndex = (langIndex - 1 + languages.Count) % languages.Count;
            updateUI();
        }

        private void saveLang()
        {
            string curlang = languages[langIndex];
            titleNames[curlang] = titlename.Text == null ? string.Empty : titlename.Text;
            titleAuthors[curlang] = titleauthor.Text == null ? string.Empty : titleauthor.Text;
        }

        private void updateUI()
        {
            List<string> selectedLangs = getSelectedLanguages();
            string curlang = languages[langIndex];
            currentLang.Text = langNames[curlang];
            titlename.Text = titleNames[curlang];
            titleauthor.Text = titleAuthors[curlang];
            try
            {
                gameico.Source = new Bitmap(icoPaths[curlang]);
            }
            catch (Exception ex)
            {
                trace("ERROR", $"Failed to load icon for {curlang}: {ex.Message}");
            }
            langStatusText.IsVisible = !selectedLangs.Contains(curlang);
        }

        public void onLangCheck(object sender, RoutedEventArgs e)
        {
            List<string> selectedLangs = getSelectedLanguages();
            string curlang = languages[langIndex];
            langStatusText.IsVisible = !selectedLangs.Contains(curlang);
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
                        string fileName = Path.GetFileName(filePath);
                        if (File.Exists(Path.Combine("Data", "Splashes", fileName)))
                            File.Delete(Path.Combine("Data", "Splashes", fileName));
                        File.Copy(filePath, Path.Combine("Data", "Splashes", fileName));
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
                            string curlang = languages[langIndex];
                            gameico.Source = bitmap;
                            string fileName = Path.GetFileName(filePath);
                            if (File.Exists(Path.Combine("Data", "Icons", fileName)))
                                File.Delete(Path.Combine("Data", "Icons", fileName));
                            File.Copy(filePath, Path.Combine("Data", "Icons", fileName));
                            icoPaths[curlang] = Path.Combine("Data", "Icons", fileName);
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

        //private async void OnBrowseKeysClicked(object sender, RoutedEventArgs e)
        //{
        //    var toplevel = TopLevel.GetTopLevel(this);
        //    var file = await toplevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        //    {
        //        Title = "Select prod.Keys",
        //        FileTypeFilter = new List<FilePickerFileType>
        //        {
        //            new FilePickerFileType("Switch Keys")
        //            {
        //                Patterns = new List<string> { "*.keys" }
        //            }
        //        },
        //        AllowMultiple = false
        //    });
        //
        //    if (file.Count > 0)
        //    {
        //        keypath.Text = file[0].Path.LocalPath;
        //        trace("INFO", $"Selected keys: {keypath.Text}");
        //    }
        //}

        private void OnGenTitleIDClicked(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();

            byte[] buffer = new byte[8];
            rand.NextBytes(buffer);
            buffer[7] = 0x01;
            ulong idVal = BitConverter.ToUInt64(buffer, 0);
            if (idVal < 0x0100000000010000)
            {
                idVal |= 0x0100000000010000;
            }
            
            titleid.Text = idVal.ToString("X16");
        }

        public void PopulateRuntimes()
        {
            var runtimes = new List<string>();
            try
            {
                string endingDir = Path.Combine("Cache", "runtimes");
                string basePath = ""; //TECHNICALLY there's no linux path.
                string ltsPath = ""; //TECHNICALLY there's no linux path.
                string lts2026Path = "";
                string betaPath = ""; 
                string devPath = "";

                switch (platform)
                {
                    case "windows":
                        basePath = Path.Combine("C:", "ProgramData", "GameMakerStudio2", endingDir);
                        ltsPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-LTS");
                        lts2026Path = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-LTS2026");
                        betaPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-Beta");
                        devPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-Dev");
                        break;
                    case "osx":
                        basePath = "/" + Path.Combine("Users", "Shared", "GameMakerStudio2", endingDir);
                        ltsPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-LTS");
                        lts2026Path = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-LTS2026");
                        betaPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-Beta");
                        devPath = basePath.Replace("GameMakerStudio2", "GameMakerStudio2-Dev");
                        break;
                    case "linux":
                        break;
                }

                if (cbUseProgramData.IsChecked == false)
                {
                    basePath = Path.Combine("Runners", "gmmonthly");
                    ltsPath = Path.Combine("Runners", "gmlts");
                    lts2026Path = Path.Combine("Runners", "gmlts2026");
                    betaPath = Path.Combine("Runners", "gmbeta");
                }

                //mainline
                if (Directory.Exists(basePath))
                    runtimes.AddRange(Directory.GetDirectories(basePath).Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Monthly").ToList());
                else
                    trace("WARN", "No Monthly Runtimes Found!");

                //LTS
                if (Directory.Exists(ltsPath))
                    runtimes.AddRange(Directory.GetDirectories(ltsPath).Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | LTS").ToList());
                else
                    trace("WARN", "No LTS Runtimes Found!");

                //LTS 2026
                if (Directory.Exists(lts2026Path))
                    runtimes.AddRange(Directory.GetDirectories(lts2026Path).Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | LTS2026").ToList());
                else
                    trace("WARN", "No LTS2026 Runtimes Found!");

                //Beta
                if (Directory.Exists(betaPath))
                    runtimes.AddRange(Directory.GetDirectories(betaPath).Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Beta").ToList());
                else
                    trace("WARN", "No Beta Runtimes Found!");

                //Nocturnus stop bothering with this lol
                //if (Directory.Exists(devPath))
                //    runtimes.AddRange(Directory.GetDirectories(devPath).Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Dev").ToList());
                //else
                //    trace("WARN", "No Dev Runtimes Found!");

                runtimesel.ItemsSource = runtimes;
            }
            catch (Exception ex)
            {
                trace("ERROR", ex.Message);
            }
        }

        private List<string> getSelectedLanguages()
        {
            var languageChecks = new (CheckBox box, string language)[]
            {
                (aeCheck, "AmericanEnglish"),
                (cfCheck, "CanadianFrench"),
                (saCheck, "LatinAmericanSpanish"),
                (bpCheck, "BrazilianPortuguese"),
                (jpCheck, "Japanese"),
                (csCheck, "SimplifiedChinese"),
                (ctCheck, "TraditionalChinese"),
                (haCheck, "Korean"),
                (beCheck, "BritishEnglish"),
                (frCheck, "French"),
                (geCheck, "German"),
                (esCheck, "Spanish"),
                (itCheck, "Italian"),
                (duCheck, "Dutch"),
                (poCheck, "Portuguese"),
                (ruCheck, "Russian")
            };
            List<string> lang = new List<string>();
            foreach (var (box, language) in languageChecks)
            {
                if (box.IsChecked == true)
                {
                    lang.Add(language);
                }
            }
            return lang;
        }

        private void OnCleanClicked(object sender, RoutedEventArgs e)
        {
            logbox.Text = "";
        }

        private async void OnExportClicked(object sender, RoutedEventArgs e)
        {
            string logContent = logbox.Text;

            if (string.IsNullOrEmpty(logContent))
            {
                trace("WARN", "Log is empty.");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export ZeusNX Log",
                SuggestedFileName = $"ZeusNX_Log_{DateTime.Now:yyyyMMdd_HHmmss}",
                FileTypeChoices = new[] {new FilePickerFileType("Text Files"){Patterns = new[]{"*.txt"}}}
            });

            if (file != null)
            {
                try
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, logContent);
                    trace("INFO", $"Log exported.");
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to export log: {ex.Message}");
                }
            }
        }

        private async void InstallKeys(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Prod.keys",
                FileTypeFilter = new[] { new FilePickerFileType("Key file") { Patterns = new[] { "*.keys" } } }
            });

            if (files.Count > 0 && files != null)
            {
                Dictionary<string, string> keyFile = new Dictionary<string, string>();
                Dictionary<int, string> allKeys = new Dictionary<int, string>();

                trace("DEBUG", files[0].TryGetLocalPath());
                string[] rawFile = File.ReadAllLines(files[0].TryGetLocalPath());
                int track = 0;
                foreach (var key in rawFile)
                {
                    keyFile.Add(key.Split(" = ")[0], key.Split(" = ")[1]);
                    allKeys.Add(track, key.Split(" = ")[0]);
                    trace("DEBUG", $"{key.Split(" = ")[0]} = {keyFile[key.Split(" = ")[0]]}");
                    track++;
                }

                foreach (var key in keyFile)
                {
                    if (key.Key.Contains("master_kek_source"))
                    {
                        string str = key.Value;
                        trace("DEBUG", $"Before: {key.Value}");
                        if (key.Value.EndsWith("00") || key.Value.Length > 32)
                            str = key.Value.Remove(key.Value.Length - 2);
                        trace("DEBUG", $"After: {str}");
                        keyFile[key.Key] = str;

                        trace("DEBUG", $"check {keyFile[key.Key]}");
                    }
                }

                string[] finalKeys = new string[keyFile.Count];
                for (int i = 0; i < keyFile.Count; i++)
                {
                    finalKeys[i] = $"{allKeys[i]} = {keyFile[allKeys[i]]}";
                }

                if (File.Exists("Data/prod.keys"))
                    File.Delete("Data/prod.keys");
                File.Create("Data/prod.keys").Close();

                File.WriteAllLines("Data/prod.keys", finalKeys);

                btnInstallKeys.Content = "Keys installed";
                btnInstallKeys.IsEnabled = false;
            }
        }

        private async void SaveMetadata(object sender, RoutedEventArgs e)
        {
            saveLang();
            if (projpath.Text == null)
            {
                trace("ERROR", "Invalid project path, cannot save metadata!");
                return;
            }
            var meta = new ZeusNXMetadata()
            {
                TitleID = titleid.Text,
                Version = titleversion.Text,
                ProjectPath = projpath.Text,
                RuntimeVerison = runtimesel.SelectedItem.ToString().Split(" | ")[0],
                ConfigName = projconf.Text,
                SplashPath = splashPath,
                ExistingOptionsCheck = existingoptionsCheck.IsChecked == true,
                RequireAccount = preselecteduserCheck.IsChecked == true,
                DebugOutput = debugCheck.IsChecked == true,
                EnableFileAccessChecking = fileaccessCheck.IsChecked == true,
                InterpolatePixels = interpolateCheck.IsChecked == true,
                Scale = scaleCheck.IsChecked == true,
                texturePage = texturesizesel.SelectedIndex,
                UseSplash = splashCheck.IsChecked == true,
                SameIcons = sameicoCheck.IsChecked == true,
                EnableScreenShots = screenshotCheck.IsChecked == true,
                EnableVideoCapture = recordCheck.IsChecked == true,
                AmericanEnglish = aeCheck.IsChecked == true,
                CanadianFrench = cfCheck.IsChecked == true,
                LatinAmericanSpanish = saCheck.IsChecked == true,
                BrazilianPortuguese = bpCheck.IsChecked == true,
                Japanese = jpCheck.IsChecked == true,
                SimplifiedChinese = csCheck.IsChecked == true,
                TraditionalChinese = ctCheck.IsChecked == true,
                Korean = haCheck.IsChecked == true,
                BritishEnglish = beCheck.IsChecked == true,
                French = frCheck.IsChecked == true,
                German = geCheck.IsChecked == true,
                EuropeanSpanish = esCheck.IsChecked == true,
                Italian = itCheck.IsChecked == true,
                Dutch = duCheck.IsChecked == true,
                Portuguese = poCheck.IsChecked == true,
                Russian = ruCheck.IsChecked == true,
                TitleNames = titleNames,
                TitleAuthors = titleAuthors,
                IconPaths = icoPaths
            };
            //var topLevel = TopLevel.GetTopLevel(this);
            //var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            //{
            //    Title = "Save ZeusNX Metadata",
            //    FileTypeChoices = new[] { new FilePickerFileType("ZeusNX Metadata") { Patterns = new[] { "*.znx" } } }
            //});

            //if (file != null)
            //{
            string json = JsonConvert.SerializeObject(meta, Formatting.Indented);
            string filePath = Path.Combine("Data", "Metadata", $"{Path.GetFileNameWithoutExtension(meta.ProjectPath)}.znx");
            try
            {            
                if (!File.Exists(filePath))
                    File.Delete(filePath);
                File.Create(filePath).Close();
                await File.WriteAllTextAsync(filePath, json);
                PopulateMetadata();
                metalist.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                trace("ERROR", $"Failed to serialize metadata: {ex.Message}");
                return;
            }
            trace("INFO", $"ZNX file saved at {filePath}.");
        }

        private async void LoadMetadata(object sender, RoutedEventArgs e)
        {
            //var topLevel = TopLevel.GetTopLevel(this);
            //var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            //{
            //    Title = "Load ZeusNX Metadata",
            //    FileTypeFilter = new[] { new FilePickerFileType("ZeusNX Metadata") { Patterns = new[] { "*.znx" } } }
            //});
            //if (files.Count > 0)
            //{
            if (metalist.ItemsSource == null || metalist.SelectedItem == null)
            {
                trace("ERROR", "No metadata file selected!");
                return;
            }

            string filePath = Path.Combine("Data", "Metadata", $"{metalist.SelectedItem}.znx");
            string json = await File.ReadAllTextAsync(filePath);
            var meta = JsonConvert.DeserializeObject<ZeusNXMetadata>(json);

            if (meta != null)
            {
                // Restore UI
                titleid.Text = meta.TitleID;
                titleversion.Text = meta.Version;
                projpath.Text = meta.ProjectPath;
                projconf.Text = meta.ConfigName;
                preselecteduserCheck.IsChecked = meta.RequireAccount;
                debugCheck.IsChecked = meta.DebugOutput;
                interpolateCheck.IsChecked = meta.InterpolatePixels;
                fileaccessCheck.IsChecked = meta.EnableFileAccessChecking;
                scaleCheck.IsChecked = meta.Scale;
                texturesizesel.SelectedIndex = meta.texturePage;
                splashCheck.IsChecked = meta.UseSplash;
                sameicoCheck.IsChecked = meta.SameIcons;
                screenshotCheck.IsChecked = meta.EnableScreenShots;
                recordCheck.IsChecked = meta.EnableVideoCapture;

                //lang
                aeCheck.IsChecked = meta.AmericanEnglish;
                cfCheck.IsChecked = meta.CanadianFrench;
                saCheck.IsChecked = meta.LatinAmericanSpanish;
                bpCheck.IsChecked = meta.BrazilianPortuguese;
                jpCheck.IsChecked = meta.Japanese;
                csCheck.IsChecked = meta.SimplifiedChinese;
                ctCheck.IsChecked = meta.TraditionalChinese;
                haCheck.IsChecked = meta.Korean;
                beCheck.IsChecked = meta.BritishEnglish;
                frCheck.IsChecked = meta.French;
                geCheck.IsChecked = meta.German;
                esCheck.IsChecked = meta.EuropeanSpanish;
                itCheck.IsChecked = meta.Italian;
                duCheck.IsChecked = meta.Dutch;
                poCheck.IsChecked = meta.Portuguese;
                ruCheck.IsChecked = meta.Russian;

                // Restore Dictionaries
                titleNames = meta.TitleNames ?? titleNames;
                titleAuthors = meta.TitleAuthors ?? titleAuthors;
                icoPaths = meta.IconPaths ?? icoPaths;
                splashPath = meta.SplashPath ?? Path.Combine("Runners", "shared", "splash_default.png");
                //load splash
                try
                {
                    gamesplash.Source = new Bitmap(splashPath);
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to load splash: {ex.Message}");
                }

                //try to auto select runtime
                if (meta.RuntimeVerison != null)
                {
                    var runtimeList = runtimesel.ItemsSource;
                    int count = 0;
                    foreach (var item in runtimeList)
                    {
                        string str = item.ToString();
                        if (str.Contains(meta.RuntimeVerison))
                        {
                            runtimesel.SelectedIndex = count;
                            break;
                        }
                        count++;
                    }
                }
                else
                    trace("WARN", "Failed to get runtime version, resave metadata!");

                updateUI(); // Refresh the display for the current language
                trace("INFO", $"ZNX file loaded from {filePath}.");
                //}
            }
        }

        private async void DeleteMetadata(object sender, RoutedEventArgs e)
        {
            string filePath = $"Data/Metadata/{metalist.SelectedItem}.znx";
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    trace("INFO", "ZNX file deleted successfully.");
                    PopulateMetadata();
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to delete ZNX file: {ex.Message}");
                }
            }
            else
            {
                //this should never actually happen.
                trace("ERROR", "Selected metadata file does not exist.");
            }
        }

        public async void BuildNSP(object sender, RoutedEventArgs e)
        {
            bool failed = false;
            try
            {
                var stopwatch = Stopwatch.StartNew();
                saveLang();
                buildnsp.IsEnabled = false;
                //support latest mainline release (2024.14.4.286) and latest lts (2022.0.3.99) on release, MAYBE beta for that one undertale thing. leave nocturnus alone since that's internal yoyogames shit
                trace("INFO", "Build START!");
                //start by checking if shit is filled out
                var projPath = projpath.Text;
                var titleID = titleid.Text == null ? null : titleid.Text.ToLower();
                var titleVer = titleversion.Text == null ? "0.0.0" : titleversion.Text;
                var projConfig = projconf.Text == null ? "Default" : projconf.Text;
                var keyPath = "Data/prod.keys"; //this is just gonna be hardcoded here since you "install" the keys anyways. if i was smart i'd just parse the keys needed and just feed that as an argument instead of having the entire file NO????????????
                List<string> selLanguages = getSelectedLanguages();

                if (!File.Exists(keyPath))
                {
                    trace("ERROR", "Keys are not installed!!!");
                    failed = true;
                    return;
                }
                if (projPath == null || !projPath.Contains(".yyp"))
                {
                    trace("ERROR", "Invalid project file!");
                    failed = true;
                    return;
                }
                if (selLanguages.Count == 0)
                {
                    trace("ERROR", "At least one language needs to be selected!");
                    failed = true;
                    return;
                }
                if (!verifyTitleID(titleID))
                {
                    failed = true;
                    return;
                }
                
                string projDir = projPath.Replace(Path.GetFileName(projPath), "");
                string projName = Path.GetFileNameWithoutExtension(projPath);
                var selectedRuntime = runtimesel.SelectedItem as string;
                var branch = selectedRuntime?.Split('|')[1].Trim();
                selectedRuntime = selectedRuntime?.Split('|')[0].Trim();
                selectedRuntime = $"runtime-{selectedRuntime}";
                string runtimePath = string.Empty;
                string commonPath = Path.Combine($"GameMakerStudio2{(branch == "Mainline" ? "" : $"-{branch}")}", "Cache", "runtimes", selectedRuntime);
                string localVersion = branch switch
                {
                    "Monthly" => "monthly",
                    "LTS" => "lts",
                    "LTS2026" => "lts2026",
                    "Beta" => "beta",
                    "Dev" => "dev"
                };

                runtimePath = platform switch
                {
                    "windows" => Path.Combine("C:", "ProgramData", commonPath, "zarfa").Replace("zarfa", ""),
                    "osx" => "/" + Path.Combine("Users", "Shared", commonPath, "zarfa").Replace("zarfa", ""),
                    "linux" => Path.Combine("Runners", $"gm{localVersion}", "zarfa").Replace("zarfa", "")
                };
                
                if (cbUseProgramData.IsChecked == false)
                    runtimePath = Path.Combine("Runners", $"gm{localVersion}", selectedRuntime, "zarfa").Replace("zarfa", "");

                if (!Directory.Exists(runtimePath))
                {
                    trace("ERROR", $"{selectedRuntime} not found. do you have the runtime installed?");
                    failed = false; //????
                    return;
                }
                //check if associated ZeusNX runtime is here, otherwise halt you kinda need those to make a build
                if (!Directory.Exists(Path.Combine("Runners",  selectedRuntime)))
                {
                    trace("ERROR", $"{selectedRuntime} files not found! Either follow the guide or check the Github repo to make sure you have it.");
                    failed = false;
                    return;
                }

                //we should check project compatibility here since running lts on a 2024 runtime will make it crash the fuck out
                //google "how to get version from yyp which is just an evil json"
                string YYP = File.ReadAllText(projPath);
                JObject jYYP = JObject.Parse(YYP);
                var yymetaData = jYYP["MetaData"];
                if (yymetaData != null && yymetaData["IDEVersion"] != null)
                {
                    string versionString = yymetaData["IDEVersion"].ToString();
                    string temp = versionString.Split('.')[0];
                    temp += "." + versionString.Split('.')[1] + "." + versionString.Split('.')[2];
                    string temp2 = selectedRuntime.Split(".")[0];
                    temp2 += "." + selectedRuntime.Split(".")[1] + "." + selectedRuntime.Split('.')[2];
                    temp2 = temp2.Replace("runtime-", string.Empty);
                    if (temp != temp2)
                    {
                        var tempNum = Double.Parse(temp.Split('.')[0]);
                        var tempNum2 = Double.Parse(temp2.Split('.')[0]);
                        if (temp == "2024.14.3" && temp2 == "2024.14.4")
                            trace("WARN", "2024.14.3 with a 2024.14.4 runtime found, this will MOST LIKELY FAIL. Update your project with the IDE!");
                        else if (tempNum2 < 2024 && tempNum >= 2024)
                        {
                            trace("ERROR", "Trying to build a 2024 project with a pre-2024 runtime will NOT work!");
                            failed = true;
                            return;
                        }
                        else if (tempNum2 >= 2024 && tempNum < 2024)
                        {
                            trace("ERROR", "Trying to build a pre-2024 project with a 2024 runtime will NOT work!");
                            failed = true;
                            return;
                        }
                        else
                            trace("WARN", "Project versions don't match, there may be dragons!");
                    }

                    //check if we need to set a prefab check or not, thanks 2024.14.
                    if (temp2.Contains("2024.14") || temp2.Contains("2026"))
                    {
                        trace("INFO", "2024.14+ project found, enabling prefab flag...");
                        enablePrefab = true;
                    }
                    else
                        enablePrefab = false;
                }

                //if the project is less than 2024 we'll add a thing for options_switch.yy, options were still in the yyp until 2023.11 i think
                if (!selectedRuntime.Contains("2024") || !selectedRuntime.Contains("2026"))
                {
                    //backup original yyp
                    File.Copy(projPath, $"{projDir}{projName}.yypbck", true);
                    var options = jYYP["Options"] as JArray;
                    if (options != null)
                    {
                        var switchEntry = options.FirstOrDefault(o => o["name"]?.ToString() == "Switch");
                        if (switchEntry == null)
                        {
                            trace("INFO", "Adding Switch entry to yyp options...");

                            options.Add(new JObject()
                        {
                            {"name", "Switch"},
                            {"path", "options/switch/options_switch.yy"}
                        });
                            File.WriteAllText(projPath, jYYP.ToString(Formatting.Indented));
                        }
                        else
                        {
                            trace("INFO", "yyp options already has a Switch entry, skipping...");
                            File.Delete($"{projDir}{projName}.yypbck");
                        }
                    }
                }

                //patch GMAssetCompiler.dll to ignore licence checks
                trace("INFO", "Patching GMAssetCompiler.dll...");
                if (File.Exists($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}"))
                {
                    File.Delete($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}");
                    File.Copy($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}", $"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}");
                    File.Delete($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}");
                }
                File.Copy($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}", $"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}");
                File.Delete($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}");
                var writerparams = new WriterParameters { DeterministicMvid = true, SymbolWriterProvider = null, };
                var assetcompiler = AssemblyDefinition.ReadAssembly($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}", new ReaderParameters { ReadWrite = true });
                YYPatch.PatchAssetCompiler(assetcompiler);
                assetcompiler.Write($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}", writerparams); //:pray:
                assetcompiler.Dispose();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //make temp directories and everything
                trace("INFO", "Creating build dir...");
                var buildDir = $"{projName}_build{DateTime.Now:yyyyMMdd_HHmmss}";
                if (!Directory.Exists(buildDir) || !Directory.EnumerateFileSystemEntries(buildDir).Any())
                {
                    Directory.CreateDirectory(buildDir);
                    Directory.CreateDirectory(Path.Combine(buildDir, "tmp"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "cache"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "exefs"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "romfs"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "romfs", "nro"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "romfs", ".nrr"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "control"));
                    Directory.CreateDirectory(Path.Combine(buildDir, "nsp", "logo"));
                }
                else
                {
                    trace("ERROR", $"{buildDir} has data inside!!");
                    failed = true;
                    return;
                }
                //copy runtime files to exefs / control
                trace("INFO", "Copying runtime files...");
                CopyDirectory(Path.Combine("Runners", selectedRuntime, "bin"), Path.Combine(buildDir, "nsp", "exefs"), true);
                CopyDirectory(Path.Combine("Runners", "shared", "logo"), Path.Combine(buildDir, "nsp", "logo"), true);

                //generate options.ini out of thin air
                var optionsINI = new IniFile();
                optionsINI["LLVM-Switch"]["SDKDir"] = "C:\\Nintendo\\NXSDK\\NintendoSDK";
                optionsINI["LLVM-Switch"]["UseNEX"] = false;
                optionsINI["LLVM-Switch"]["UseNPLN"] = false;
                optionsINI["LLVM-Switch"]["nMeta"] = "C:\\Users\\ZeusNX\\Project\\options\\switch\\application.nmeta";
                optionsINI.Save(Path.Combine(buildDir, "nsp", "romfs", "options.ini"));

                //make the preselecteduser file
                File.Create(Path.Combine(buildDir, "nsp", "romfs", "preselected_user")).Close(); 
                File.WriteAllText(Path.Combine(buildDir, "nsp", "romfs", "preselected_user"), (preselecteduserCheck.IsChecked == true ? "True" : "False"));

                //options_switch.yy creation can you believe this shit actually works
                if (!Directory.Exists(Path.Combine(projDir, "options", "switch")))
                    Directory.CreateDirectory(Path.Combine(projDir, "options", "switch"));

                if (File.Exists(Path.Combine(projDir, "options", "switch", "options_switch.yy")) && existingoptionsCheck.IsChecked == true)
                    trace("INFO", "Using existing options_switch.yy...");
                else
                {
                    if (File.Exists(Path.Combine(projDir, "options", "switch", "options_switch.yy")))
                    {
                        trace("WARN", "Existing options_switch.yy found, backing up incase of user error...");
                        File.Copy(Path.Combine(projDir, "options", "switch", "options_switch.yy"), Path.Combine(projDir, "options", "switch", "options_switch.bak"), true);
                    }
                    else if (existingoptionsCheck.IsChecked == true)
                        trace("WARN", "Existing options checked, but there's no options_switch.yy dingus.");

                    trace("INFO", "Creating options_switch.yy...");
                    //ok so funny thing is all fields we can modify are actually universal for both 2024 and whatever came before, iPhones are AWESOME!
                    //lowkey should refactor this to only have one, it's the exact same code for both except for class
                    if (selectedRuntime.Contains("2024") || selectedRuntime.Contains("2026"))
                    {
                        //default for now, we're gonna add some stuff later for it
                        YYSwitchOptions2024 options = new YYSwitchOptions2024
                        {
                            option_switch_allow_debug_output = debugCheck.IsChecked == true ? true : false,
                            option_switch_enable_fileaccess_checking = fileaccessCheck.IsChecked == true ? true : false,
                            option_switch_interpolate_pixels = interpolateCheck.IsChecked == true ? true : false,
                            option_switch_project_nmeta = Path.Combine(projDir, "options", "switch", "application.nmeta"), //default path, honestly this is supposed to NOT be used since NintendoSDK is kinda GULP behind locked doors. we're using other stuff for nsp metadata anyways.
                            option_switch_scale = scaleCheck.IsChecked == true ? 0 : 1, //0 is keep aspect ration, 1 is full scale.
                            option_switch_splash_screen = Path.Combine(projDir, "options", "switch", "splash.png"), //if one is used i guess, but that kinda ignores our own toggle. think about it melia.
                            option_switch_texture_page = texturesizesel.SelectedItem as string, //there's only 7 options i'll deal with that in the project settings tab
                            option_switch_use_splash = splashCheck.IsChecked == true ? true : false //i s'pose
                        };
                        File.WriteAllText(Path.Combine(projDir, "options", "switch", "options_switch.yy"), JsonConvert.SerializeObject(options, Formatting.Indented));
                    }
                    else
                    {
                        //add a case for yyp checking here, gonna need to be EVIL about it
                        YYSwitchOptionsLTS options = new YYSwitchOptionsLTS
                        {
                            option_switch_allow_debug_output = debugCheck.IsChecked == true ? true : false,
                            option_switch_enable_fileaccess_checking = fileaccessCheck.IsChecked == true ? true : false,
                            option_switch_interpolate_pixels = interpolateCheck.IsChecked == true ? true : false,
                            option_switch_project_nmeta = Path.Combine(projDir, "options", "switch", "application.nmeta"), //default path, honestly this is supposed to NOT be used since NintendoSDK is kinda GULP behind locked doors. we're using other stuff for nsp metadata anyways.
                            option_switch_scale = scaleCheck.IsChecked == true ? 0 : 1, //0 is keep aspect ration, 1 is full scale.
                            option_switch_splash_screen = Path.Combine(projDir, "options", "switch", "splash.png"), //if one is used i guess, but that kinda ignores our own toggle. think about it melia.
                            option_switch_texture_page = texturesizesel.SelectedItem as string, //there's only 7 options i'll deal with that in the project settings tab
                            option_switch_use_splash = splashCheck.IsChecked == true ? true : false //i s'pose
                        };
                        File.WriteAllText(Path.Combine(projDir, "options", "switch", "options_switch.yy"), JsonConvert.SerializeObject(options, Formatting.Indented));
                    }
                }

                //check for cache toggle here
                string cacheDir = String.Empty;
                if (cbUseGMCache.IsChecked == true)
                {
                    if (Directory.Exists(Path.Combine("Cache", projName)))
                        Directory.CreateDirectory(Path.Combine("Cache", projName));
                    cacheDir = Path.Combine("Cache", projName);
                }
                else
                    cacheDir = Path.Combine(buildDir, "cache");

                trace("INFO", "Preprocessing GMS2 project...");
                if (await runCompiler(runtimePath, projPath, projName, buildDir, cacheDir, projConfig, true) >= 2)
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                trace("INFO", "Compiling GMS2 project...");
                if (await runCompiler(runtimePath, projPath, projName, buildDir, cacheDir, projConfig, false) >= 2)
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //extension setup/debug extension check
                string[] extensions = Directory.GetFiles(Path.Combine(buildDir, "nsp", "romfs"), "*.nro");
                if (extensions.Length > 0)
                {
                    trace("DEBUG", "Extensions found!");
                    string seperator = platform == "windows" ? "\\" : "/";
                    foreach (string extension in extensions)
                    {                      
                        string extensionName = extension.Split(seperator)[extension.Split(seperator).Length - 1];
                        extensionName = extensionName.Split(".")[0];
                        trace("DEBUG", "Extension: " + extensionName);
                        File.Move(Path.Combine(buildDir, "nsp", "romfs", extensionName + ".nro"), Path.Combine(buildDir, "nsp", "romfs", "nro", extensionName + ".nro"));
                        //try for nrr file
                        if (File.Exists(Path.Combine(buildDir, "nsp", "romfs", $"{extensionName}.nrr")))
                            File.Move(Path.Combine(buildDir, "nsp", "romfs", extensionName + ".nrr"), Path.Combine(buildDir, "nsp", "romfs", ".nrr", extensionName + ".nrr"));
                    }
                }
                if (debugCheck.IsChecked == true)
                {
                    File.Copy(Path.Combine("Runners", selectedRuntime, "lib", "YYSwitchOutputLib.nro"), Path.Combine(buildDir, "nsp", "romfs", "nro", "YYSwitchOutputLib.nro"));
                    File.Copy(Path.Combine("Runners", selectedRuntime, "lib", "YYSwitchOutputLib.nrr"), Path.Combine(buildDir, "nsp", "romfs", ".nrr", "YYSwitchOutputLib.nrr"));
                }

                //now starts the fun part, copy over selected icon
                trace("INFO", "Copying over icon(s)...");
                foreach (var lang in selLanguages)
                {
                    if (sameicoCheck.IsChecked == true)
                        File.Copy(icoPaths["AmericanEnglish"], Path.Combine(buildDir, "nsp", "control", $"icon_{lang}.dat"), true);
                    else
                        File.Copy(icoPaths[lang], Path.Combine(buildDir, "nsp", "control", $"icon_{lang}.dat"), true);
                }

                //copy over splash if toggle is set
                if (splashCheck.IsChecked == true && gamesplash.Source != null)
                {
                    trace("INFO", "Copying over splash...");
                    var bitmap = gamesplash.Source as Bitmap;
                    using (var stream = File.OpenWrite(Path.Combine(buildDir, "nsp", "romfs", "splash.png")))
                    {
                        bitmap.Save(stream);
                    }
                }

                //generate xml for hptnacp
                trace("INFO", "Generating control.nacp...");
                List<Title> langList = new List<Title>();
                foreach (var language in selLanguages)
                {
                    langList.Add(new Title
                    {
                        Language = language,
                        Name = titleNames[language],
                        Publisher = titleAuthors[language]
                    });
                }
                Application nacpXML = new Application
                {
                    Title = langList,
                    StartupUserAccount = preselecteduserCheck.IsChecked == true ? "Required" : "None",
                    SupportedLanguage = selLanguages,
                    Screenshot = screenshotCheck.IsChecked == true ? "Allow" : "Deny",
                    VideoCapture = recordCheck.IsChecked == true ? "Enable" : "Disable",
                    PresenceGroupId = $"0x{titleID}",
                    DisplayVersion = titleVer,
                    SaveDataOwnerId = $"0x{titleID}",
                    AddOnContentBaseId = $"0x{titleID}",
                    LocalCommunicationId = $"0x{titleID}",
                    SeedForPseudoDeviceId = $"0x{titleID}"
                };
                XmlSerializer serializer = new XmlSerializer(typeof(Application));
                using (FileStream fs = new FileStream(Path.Combine(buildDir, "tmp", "control.xml"), FileMode.Create))
                    serializer.Serialize(fs, nacpXML);

                string hptnacpArgs = $"-i \"{Path.Combine(buildDir, "tmp", "control.xml")}\" -o \"{Path.Combine(buildDir, "nsp", "control", "control.nacp")}\" -a createnacp";
                if (await runExternalTool(Path.Combine("Tools", platform, $"hptnacp{(platform == "windows" ? ".exe" : "")}"), hptnacpArgs, "HPTNACP", true, true) != 0)
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //pack nsp
                trace("INFO", "Building NSP...");
                string hpArgs = $"-k \"{keyPath}\" --tempdir \"{Path.Combine(buildDir, "hactmp")}\" --backupdir \"{Path.Combine(buildDir, "cache")}\" --ncadir \"{Path.Combine(buildDir, "cache", "nca")}\" --nspdir \"{buildDir}\" --exefsdir \"{Path.Combine(buildDir, "nsp", "exefs")}\" --controldir \"{Path.Combine(buildDir, "nsp", "control")}\" --logodir \"{Path.Combine(buildDir, "nsp", "logo")}\" --romfsdir \"{Path.Combine(buildDir, "nsp", "romfs")}\"";
                //if (offlineManualPath.Text != null && offlineManualPath.Text != string.Empty)
                //    hpArgs += $" --htmldocdir \"{offlineManualPath.Text}\"";
                hpArgs += $" --titleid \"{titleID}\"";
                if (await runExternalTool(Path.Combine("Tools", platform, $"hacbrewpack{(platform == "windows" ? ".exe" : "")}"), hpArgs, "HBP", true, true) != 0)
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //cleanup
                trace("INFO", "Restoring GMAssetCompiler.dll");
                File.Delete($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}");
                File.Copy($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}", $"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.dll")}");
                File.Delete($"{runtimePath}{Path.Combine(compilerPath, "GMAssetCompiler.bak")}");
                trace("INFO", "Deleting all temp files...");
                if (Directory.Exists(Path.Combine(buildDir, "tmp")))
                    Directory.Delete(Path.Combine(buildDir, "tmp"), true);
                if (Directory.Exists(Path.Combine(buildDir, "cache")))
                    Directory.Delete(Path.Combine(buildDir, "cache"), true);
                if (Directory.Exists(Path.Combine(buildDir, "nsp")))
                    Directory.Delete(Path.Combine(buildDir, "nsp"), true);
                stopwatch.Stop();
                TimeSpan time = stopwatch.Elapsed;
                try
                {
                    if (Directory.Exists(buildDir))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = buildDir,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                        trace("INFO", "Opened build directory.");
                    }
                }
                catch (Exception ex)
                {
                    trace("ERROR", $"Failed to open build dir: {ex.Message}");
                }
                trace("INFO", $"Build Complete in {time.TotalSeconds:F2}s!");
            }
            catch (Exception ex)
            {
                trace("ERROR", $"UNHANDLED FATAL ERROR: {ex.Message}");
            }
            finally
            {
                if (failed)
                    trace("ERROR", $"Build Failed!");
                buildnsp.IsEnabled = true;
            }
        }

        private async Task<int> runExternalTool(string fileName, string args, string prefix, bool outputLog = false, bool outputError = false)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = new Process { StartInfo = psi };
                if (outputLog)
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) trace(prefix, e.Data); };
                if (outputError)
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) trace($"{prefix}ERR", e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    trace("ERROR", $"{prefix} failed with exit code {process.ExitCode}");
                return process.ExitCode;
            });
        }

        private async Task<int> runCompiler(string runtimePath, string projPath, string projName, string buildDir, string cacheDir, string config, bool isPreprocess)
        {
            bool lts2026 = runtimePath.Contains("2026");
            string prefabPath = string.Empty;
            switch (platform)
            {
                case "windows": 
                    prefabPath = Path.Combine("C:", "ProgramData", $"GameMakerStudio2{(lts2026 ? "-LTS2026" : "")}", "Prefabs");
                    break;
                case "osx":
                    prefabPath = "/" + Path.Combine("Users", "Shared", $"GameMakerStudio2{(lts2026 ? "-LTS2026" : "")}", "Prefabs");
                    break;
                case "linux":
                    prefabPath = linuxBasePath + Path.Combine(".local", "share", $"GameMakerStudio2{(lts2026 ? "-LTS2026" : "")}", "Prefabs"); //i lowkey forgot about this here uhh Oops!
                    break;               
            }

            string absolutePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            string args = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{projName}\" /td=\"{Path.Combine(buildDir, "tmp")}\" /cd=\"{Path.Combine(absolutePath,cacheDir)}\" /rtp=\"{runtimePath.Remove(runtimePath.Length - 1)}\" ";
            if (enablePrefab)
                args += "/prefabs=\"" + prefabPath + "\" ";
            args += $"/m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /cfg=\"{config}\" /o=\"{Path.Combine(absolutePath, buildDir, "nsp", "romfs")}\" \"{projPath}\" ";

            trace("INFO", $"GMAC ARGS: {args}");
            trace("DEBUG", $"runCompiler args, runtimePath-{runtimePath}, projPath-{projPath}, projName-{projName}, config-{config}, isPreprocess-{(isPreprocess ? "true" : "false")}");

            if (isPreprocess) args += $"/preprocess=\"{Path.Combine(buildDir, "cache")}\"";

            return await runExternalTool($"{runtimePath}{Path.Combine(compilerPath, $"GMAssetCompiler{(platform == "windows" ? ".exe" : "")}")}", args, "GMAC", true, true); //atleast test
        }

        private bool verifyTitleID(string titleID)
        {
            if (string.IsNullOrEmpty(titleID) || titleID.Length != 16)
            {
                trace("ERROR", "Title ID must be exactly 16 hex characters!");
                return false;
            }

            if (ulong.TryParse(titleID, System.Globalization.NumberStyles.HexNumber, null, out ulong idValue))
            {
                ulong minID = 0x0100000000010000;
                ulong maxID = 0x01FFFFFFFFFFFFFF;

                if (idValue < minID || idValue > maxID)
                {
                    trace("ERROR", "Title ID is out of the valid Application range (0100000000010000 - 01FFFFFFFFFFFFFF)!");
                    return false;
                }
            }
            else
            {
                //this should never happen, but just in case
                trace("ERROR", "Title ID contains invalid characters! Use 0-9 and A-F only.");
                return false;
            }

            return true;
        }

        private void saveOption(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            var ini = new IniFile();
            ini.Load(Path.Combine("Data", "config.ini"));
            trace("DEBUG", $"Name: {cb.Name}, Checked: {cb.IsChecked == true}");
            ini["Config"][cb.Name.Replace("cb", "")] = cb.IsChecked == true;
            ini.Save(Path.Combine("Data", "config.ini"));
            if (cb.Name == "cbUseProgramData")
                PopulateRuntimes();
        }
    }
}