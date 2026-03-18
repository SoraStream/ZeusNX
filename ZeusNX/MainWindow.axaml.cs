using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private string ZeusNXVersion = "1.0.0RC3 ";
        private int langIndex = 0;
        public bool enablePrefab = false;
        public string compilerPath = "\\bin\\assetcompiler\\windows\\x64"; //append to runtime path.
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
        public Dictionary<string, string> langNames = new Dictionary<string, string>();
        public Dictionary<string, string> icoPaths = new Dictionary<string, string>();
        public Dictionary<string, string> titleNames = new Dictionary<string, string>();
        public Dictionary<string, string> titleAuthors = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            InitDict();
            PopulateRuntimes();
            trace("INFO", $"Welcome to ZeusNX, Version {ZeusNXVersion}");
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
            try
            {
                logbox.Text += $"[{type}]: {message}\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update logbox: {ex.Message}");
            }

        }

        private void InitDict()
        {
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
            //icon stuff
            icoPaths.Add("AmericanEnglish", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("CanadianFrench", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("LatinAmericanSpanish", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("BrazilianPortuguese", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Japanese", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("SimplifiedChinese", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("TraditionalChinese", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Korean", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("BritishEnglish", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("French", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("German", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Spanish", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Italian", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Dutch", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Portuguese", "Runners\\shared\\ico_default.jpg");
            icoPaths.Add("Russian", "Runners\\shared\\ico_default.jpg");
            //title stuff
            titleNames.Add("AmericanEnglish", "ZeusNX Application");
            titleNames.Add("CanadianFrench", "ZeusNX Application");
            titleNames.Add("LatinAmericanSpanish", "ZeusNX Application");
            titleNames.Add("BrazilianPortuguese", "ZeusNX Application");
            titleNames.Add("Japanese", "ZeusNX Application");
            titleNames.Add("SimplifiedChinese", "ZeusNX Application");
            titleNames.Add("TraditionalChinese", "ZeusNX Application");
            titleNames.Add("Korean", "ZeusNX Application");
            titleNames.Add("BritishEnglish", "ZeusNX Application");
            titleNames.Add("French", "ZeusNX Application");
            titleNames.Add("German", "ZeusNX Application");
            titleNames.Add("Spanish", "ZeusNX Application");
            titleNames.Add("Italian", "ZeusNX Application");
            titleNames.Add("Dutch", "ZeusNX Application");
            titleNames.Add("Portuguese", "ZeusNX Application");
            titleNames.Add("Russian", "ZeusNX Application");
            //publisher stuff
            titleAuthors.Add("AmericanEnglish", "ZeusNX User");
            titleAuthors.Add("CanadianFrench", "ZeusNX User");
            titleAuthors.Add("LatinAmericanSpanish", "ZeusNX User");
            titleAuthors.Add("BrazilianPortuguese", "ZeusNX User");
            titleAuthors.Add("Japanese", "ZeusNX User");
            titleAuthors.Add("SimplifiedChinese", "ZeusNX User");
            titleAuthors.Add("TraditionalChinese", "ZeusNX User");
            titleAuthors.Add("Korean", "ZeusNX User");
            titleAuthors.Add("BritishEnglish", "ZeusNX User");
            titleAuthors.Add("French", "ZeusNX User");
            titleAuthors.Add("German", "ZeusNX User");
            titleAuthors.Add("Spanish", "ZeusNX User");
            titleAuthors.Add("Italian", "ZeusNX User");
            titleAuthors.Add("Dutch", "ZeusNX User");
            titleAuthors.Add("Portuguese", "ZeusNX User");
            titleAuthors.Add("Russian", "ZeusNX User");

            //init everything using first lang
            currentLang.Text = langNames["AmericanEnglish"];
            gameico.Source = new Bitmap(icoPaths["AmericanEnglish"]);
            gamesplash.Source = new Bitmap("Runners\\shared\\splash_default.png");
            titlename.Text = titleNames["AmericanEnglish"];
            titleauthor.Text = titleAuthors["AmericanEnglish"];
            texturesizesel.ItemsSource = txtPageList;
            texturesizesel.SelectedIndex = 3;
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
            gameico.Source = new Bitmap(icoPaths[curlang]);
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
                            icoPaths[curlang] = filePath;
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

        private void PopulateRuntimes()
        {
            var runtimes = new List<string>();
            try
            {
                //mainline
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Mainline").ToList());
                else
                    trace("ERROR", "No Mainline Runtimes Found!");

                //LTS
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-LTS\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-LTS\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | LTS").ToList());
                else
                    trace("WARN", "No LTS Runtimes Found!");

                //Beta
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-Beta\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-Beta\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Beta").ToList());
                else
                    trace("WARN", "No Beta Runtimes Found!");

                //Nocturnus
                if (Directory.Exists("C:\\ProgramData\\GameMakerStudio2-Dev\\Cache\\runtimes"))
                    runtimes.AddRange(Directory.GetDirectories("C:\\ProgramData\\GameMakerStudio2-Dev\\Cache\\runtimes").Select(Path.GetFileName).Select(name => $"{name.Replace("runtime-", "")} | Dev").ToList());
                else
                    trace("WARN", "No Dev Runtimes Found!");

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

        private async void SaveMetadata(object sender, RoutedEventArgs e)
        {
            saveLang();
            var meta = new ZeusNXMetadata()
            {
                TitleID = titleid.Text,
                Version = titleversion.Text,
                ProjectPath = projpath.Text,
                KeysPath = keypath.Text,
                ConfigName = projconf.Text,
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
                OfflineManualPath = offlineManualPath.Text,
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
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save ZeusNX Metadata",
                FileTypeChoices = new[] { new FilePickerFileType("ZeusNX Metadata") { Patterns = new[] { "*.znx" } } }
            });

            if (file != null)
            {
                string json = JsonConvert.SerializeObject(meta, Formatting.Indented);
                await File.WriteAllTextAsync(file.Path.LocalPath, json);
                trace("INFO", "Metadata saved successfully.");
            }
        }

        private async void LoadMetadata(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load ZeusNX Metadata",
                FileTypeFilter = new[] { new FilePickerFileType("ZeusNX Metadata") { Patterns = new[] { "*.znx" } } }
            });
            if (files.Count > 0)
            {
                string json = await File.ReadAllTextAsync(files[0].Path.LocalPath);
                var meta = JsonConvert.DeserializeObject<ZeusNXMetadata>(json);

                if (meta != null)
                {
                    // Restore UI
                    titleid.Text = meta.TitleID;
                    titleversion.Text = meta.Version;
                    projpath.Text = meta.ProjectPath;
                    keypath.Text = meta.KeysPath;
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
                    offlineManualPath.Text = meta.OfflineManualPath;

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

                    updateUI(); // Refresh the display for the current language
                    trace("INFO", "Metadata loaded successfully.");
                }
            }
        }

        public async void BuildNSP(object sender, RoutedEventArgs e)
        {
            bool failed = false;
            try
            {            
                saveLang();
                buildnsp.IsEnabled = false;
                //TODO uhhh add detection for pre 2.3 projects and pre 2024 projects, formats for the options_switch.yy is different
                //support latest mainline release (2024.14.3.260) and latest lts (2022.0.3.99) on release, MAYBE beta for that one undertale thing. leave nocturnus alone since that's internal yoyogames shit
                trace("INFO", "Build START!");
                //start by checking if shit is filled out
                var projPath = projpath.Text;
                var titleID = titleid.Text == null ? null : titleid.Text.ToLower();
                var titleVer = titleversion.Text == null ? "0.0.0" : titleversion.Text;
                var projConfig = projconf.Text == null ? "Default" : projconf.Text;
                var keyPath = keypath.Text;
                List<string> selLanguages = getSelectedLanguages();

                if (keyPath == null || !keyPath.Contains(".keys"))
                {
                    trace("ERROR", "Keys not found!!!");
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

                string[] tempStr = projPath.Split('\\');
                string projDir = projPath.Replace("\\" + tempStr[tempStr.Length - 1], "");
                string projName = tempStr[tempStr.Length - 1].Replace(".yyp", "");
                var selectedRuntime = runtimesel.SelectedItem as string;
                var branch = selectedRuntime?.Split('|')[1].Trim();
                selectedRuntime = selectedRuntime?.Split('|')[0].Trim();
                selectedRuntime = $"runtime-{selectedRuntime}";
                var runtimePath = $"C:\\ProgramData\\GameMakerStudio2{(branch == "Mainline" ? "" : $"-{branch}")}\\Cache\\runtimes\\{selectedRuntime}";

                if (!Directory.Exists(runtimePath))
                {
                    trace("ERROR", $"{selectedRuntime} not found. do you have the runtime installed?");
                    failed = false;
                    return;
                }
                //check if associated ZeusNX runtime is here, otherwise halt you kinda need those to make a build
                if (!Directory.Exists($"Runners\\{selectedRuntime}"))
                {
                    trace("ERROR", $"{selectedRuntime} files not found! Either follow the guide or check the Github repo to make sure you have it.");
                    failed = true;
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
                    temp += "." + versionString.Split('.')[1];
                    string temp2 = selectedRuntime.Split(".")[0];
                    temp2 += "." + selectedRuntime.Split(".")[1];
                    temp2 = temp2.Replace("runtime-", string.Empty);
                    if (temp != temp2)
                    {
                        if (!temp2.Contains("2024") && temp.Contains("2024"))
                        {
                            trace("ERROR", "Trying to build a 2024 project with a pre-2024 runtime will NOT work!");
                            failed = true;
                            return;
                        }
                        else if (temp2.Contains("2024") && !temp.Contains("2024"))
                        {
                            trace("ERROR", "Trying to build a pre-2024 project with a 2024 runtime will NOT work!");
                            failed = true;
                            return;
                        }
                        else
                            trace("WARN", "Project versions don't match, there may be dragons!");
                    }

                    //check if we need to set a prefab check or not, thanks 2024.14.
                    if (temp2.Contains("2024.14"))
                    {
                        trace("INFO", "2024.14+ project found, enabling prefab flag...");
                        enablePrefab = true;
                    }
                    else
                        enablePrefab = false;
                }

                //if the project is less than 2024 we'll add a thing for options_switch.yy, options were still in the yyp until 2023.11 i think
                if (!selectedRuntime.Contains("2024"))
                {
                    //backup original yyp
                    File.Copy(projPath, $"{projDir}\\{projName}.yypbck", true);
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
                            File.Delete($"{projDir}\\{projName}.yypbck");
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
                if (!await runExternalTool("Tools\\xdelta.exe", $"-d -s \"{runtimePath}{compilerPath}\\GMAssetCompiler.bak\" \"Runners\\patches\\{selectedRuntime}.xdelta\" \"{runtimePath}{compilerPath}\\GMAssetCompiler.dll\"", "XDELTA"))
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //make temp directories and everything
                trace("INFO", "Creating build dir...");
                var time = DateTime.Now.ToString();
                time = time.Replace(" ", "");
                time = time.Replace(":", ".");
                time = time.Replace("-", ".");
                var buildDir = $"{projName}_build{time}";
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
                    failed = true;
                    return;
                }
                //copy runtime files to exefs / control
                trace("INFO", "Copying runtime files...");
                CopyDirectory($"Runners\\{selectedRuntime}\\bin", $"{buildDir}\\nsp\\exefs", true);
                CopyDirectory($"Runners\\shared\\logo", $"{buildDir}\\nsp\\logo", true);

                //generate options.ini out of thin air
                var optionsINI = new IniFile();
                optionsINI["LLVM-Switch"]["SDKDir"] = "C:\\Nintendo\\NXSDK\\NintendoSDK";
                optionsINI["LLVM-Switch"]["UseNEX"] = false;
                optionsINI["LLVM-Switch"]["UseNPLN"] = false;
                optionsINI["LLVM-Switch"]["nMeta"] = "C:\\Users\\ZeusNX\\Project\\options\\switch\\application.nmeta";
                optionsINI.Save($"{buildDir}\\nsp\\romfs\\options.ini");

                //make the preselecteduser file
                File.Create($"{buildDir}\\nsp\\romfs\\preselected_user").Close();
                if (preselecteduserCheck.IsChecked == true)
                    File.WriteAllText($"{buildDir}\\nsp\\romfs\\preselected_user", "False");
                else
                    File.WriteAllText($"{buildDir}\\nsp\\romfs\\preselected_user", "True");

                //options_switch.yy creation can you believe this shit actually works
                if (!Directory.Exists($"{projDir}\\options\\switch"))
                    Directory.CreateDirectory($"{projDir}\\options\\switch");

                if (File.Exists($"{projDir}\\options\\switch\\options_switch.yy") && existingoptionsCheck.IsChecked == true)
                    trace("INFO", "Using existing options_switch.yy...");
                else
                {
                    if (File.Exists($"{projDir}\\options\\switch\\options_switch.yy"))
                    {
                        trace("WARN", "Existing options_switch.yy found, backing up incase of user error...");
                        File.Copy($"{projDir}\\options\\switch\\options_switch.yy", $"{projDir}\\options\\switch\\options_switch.bak", true);
                    }
                    else if (existingoptionsCheck.IsChecked == true)
                        trace("WARN", "Existing options checked, but there's no options_switch.yy dingus.");

                    trace("INFO", "Creating options_switch.yy...");
                    //ok so funny thing is all fields we can modify are actually universal for both 2024 and whatever came before, iPhones are AWESOME!
                    if (selectedRuntime.Contains("2024"))
                    {
                        //default for now, we're gonna add some stuff later for it
                        YYOptions2024 options = new YYOptions2024
                        {
                            option_switch_allow_debug_output = debugCheck.IsChecked == true ? true : false,
                            option_switch_enable_fileaccess_checking = fileaccessCheck.IsChecked == true ? true : false,
                            option_switch_interpolate_pixels = interpolateCheck.IsChecked == true ? true : false,
                            option_switch_project_nmeta = $"{projDir}\\options\\switch\\application.nmeta", //default path, honestly this is supposed to NOT be used since NintendoSDK is kinda GULP behind locked doors. we're using other stuff for nsp metadata anyways.
                            option_switch_scale = scaleCheck.IsChecked == true ? 0 : 1, //0 is keep aspect ration, 1 is full scale.
                            option_switch_splash_screen = $"{projDir}\\options\\switch\\splash.png", //if one is used i guess, but that kinda ignores our own toggle. think about it melia.
                            option_switch_texture_page = texturesizesel.SelectedItem as string, //there's only 7 options i'll deal with that in the project settings tab
                            option_switch_use_splash = splashCheck.IsChecked == true ? true : false //i s'pose
                        };
                        File.WriteAllText($"{projDir}\\options\\switch\\options_switch.yy", JsonConvert.SerializeObject(options, Formatting.Indented));
                    }
                    else
                    {
                        //add a case for yyp checking here, gonna need to be EVIL about it
                        YYOptionsLTS options = new YYOptionsLTS
                        {
                            option_switch_allow_debug_output = debugCheck.IsChecked == true ? true : false,
                            option_switch_enable_fileaccess_checking = fileaccessCheck.IsChecked == true ? true : false,
                            option_switch_interpolate_pixels = interpolateCheck.IsChecked == true ? true : false,
                            option_switch_project_nmeta = $"{projDir}\\options\\switch\\application.nmeta", //default path, honestly this is supposed to NOT be used since NintendoSDK is kinda GULP behind locked doors. we're using other stuff for nsp metadata anyways.
                            option_switch_scale = scaleCheck.IsChecked == true ? 0 : 1, //0 is keep aspect ration, 1 is full scale.
                            option_switch_splash_screen = $"{projDir}\\options\\switch\\splash.png", //if one is used i guess, but that kinda ignores our own toggle. think about it melia.
                            option_switch_texture_page = texturesizesel.SelectedItem as string, //there's only 7 options i'll deal with that in the project settings tab
                            option_switch_use_splash = splashCheck.IsChecked == true ? true : false //i s'pose
                        };
                        File.WriteAllText($"{projDir}\\options\\switch\\options_switch.yy", JsonConvert.SerializeObject(options, Formatting.Indented));
                    }
                }

                trace("INFO", "Preprocessing GMS2 project...");
                if (!await runCompiler(runtimePath, projPath, projName, buildDir, projConfig, true))
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                trace("INFO", "Compiling GMS2 project...");
                if (!await runCompiler(runtimePath, projPath, projName, buildDir, projConfig, false))
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //now starts the fun part, copy over selected icon
                trace("INFO", "Copying over icon(s)...");
                foreach (var lang in selLanguages)
                {
                    if (sameicoCheck.IsChecked == true)
                        File.Copy(icoPaths["AmericanEnglish"], $"{buildDir}\\nsp\\control\\icon_{lang}.dat", true);
                    else
                        File.Copy(icoPaths[lang], $"{buildDir}\\nsp\\control\\icon_{lang}.dat", true);
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
                using (FileStream fs = new FileStream($"{buildDir}\\tmp\\control.xml", FileMode.Create))
                    serializer.Serialize(fs, nacpXML);

                string hptnacpArgs = $"-i \"{buildDir}\\tmp\\control.xml\" -o \"{buildDir}\\nsp\\control\\control.nacp\" -a createnacp";
                if (!await runExternalTool("Tools\\hptnacp.exe", hptnacpArgs, "HPTNACP"))
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //pack nsp
                trace("INFO", "Building NSP...");
                string hpArgs = $"-k \"{keyPath}\" --tempdir \"{buildDir}\\hactmp\" --backupdir \"{buildDir}\\cache\" --ncadir \"{buildDir}\\cache\\nca\" --nspdir \"{buildDir}\" --exefsdir \"{buildDir}\\nsp\\exefs\" --controldir \"{buildDir}\\nsp\\control\" --logodir \"{buildDir}\\nsp\\logo\" --romfsdir \"{buildDir}\\nsp\\romfs\"";
                //if (offlineManualPath.Text != null && offlineManualPath.Text != string.Empty)
                //    hpArgs += $" --htmldocdir \"{offlineManualPath.Text}\"";
                hpArgs += $" --titleid \"{titleID}\"";
                if (!await runExternalTool("Tools\\hacbrewpack.exe", hpArgs, "HBP"))
                {
                    failed = true;
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                //cleanup
                trace("INFO", "Restoring GMAssetCompiler.dll");
                File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
                File.Copy($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak", $"{runtimePath}{compilerPath}\\GMAssetCompiler.dll");
                File.Delete($"{runtimePath}{compilerPath}\\GMAssetCompiler.bak");
                trace("INFO", "Deleting all temp files...");
                if (Directory.Exists($"{buildDir}\\tmp"))
                    Directory.Delete($"{buildDir}\\tmp", true);
                if (Directory.Exists($"{buildDir}\\cache"))
                    Directory.Delete($"{buildDir}\\cache", true);
                if (Directory.Exists($"{buildDir}\\nsp"))
                    Directory.Delete($"{buildDir}\\nsp", true);
                trace("INFO", "Build Complete!");
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

        private async Task<bool> runExternalTool(string fileName, string args, string prefix)
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
            process.OutputDataReceived += (s, e) => { if (e.Data != null) Dispatcher.UIThread.InvokeAsync(() => trace(prefix, e.Data)); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) Dispatcher.UIThread.InvokeAsync(() => trace($"{prefix}ERR", e.Data)); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                trace("ERROR", $"{prefix} failed with exit code {process.ExitCode}");
                return false;
            }
            return true;
        }

        private async Task<bool> runCompiler(string runtimePath, string projPath, string projName, string buildDir, string config, bool isPreprocess)
        {
            string absolutePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory); //System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string args = $"/c /v /zpex /mv=1 /iv=0 /rv=0 /bv=0 /j=9 /gn=\"{projName}\" /td=\"{buildDir}\\tmp\" /cd=\"{buildDir}\\cache\" /rtp=\"{runtimePath}\" ";
            if (enablePrefab)
                args += "/prefabs=\"C:\\ProgramData\\GameMakerStudio2\\Prefabs\" ";
            args += $"/m=switch /tgt=144115188075855872 /cvm /bt=\"exe\" /rt=vm /cfg=\"{config}\" /o=\"{absolutePath}\\{buildDir}\\nsp\\romfs\" \"{projPath}\" ";

            trace("INFO", $"GMAC ARGS: {args}");
            //trace("DEBUG", $"runCompiler args, runtimePath-{runtimePath}, projPath-{projPath}, projName-{projName}, config-{config}, isPreprocess-{(isPreprocess ? "true" : "false")}");

            if (isPreprocess) args += $"/preprocess=\"{buildDir}\\cache\"";

            return await runExternalTool($"{runtimePath}{compilerPath}\\GMAssetCompiler.exe", args, "GMAC");
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
    }
}