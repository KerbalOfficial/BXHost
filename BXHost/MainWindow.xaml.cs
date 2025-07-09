using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

#pragma warning disable CS8602

namespace BXHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string applicationName = "BXHost";
        public const string applicationDescription = "Host or join a Self-Hosted ROBLOX Server!";
        public const string applicationVersion = "1.0";
        public const string applicationGithubUrl = "https://github.com/KerbalOfficial/BXHost/tree/main";

        public string applicationFullName=>(applicationName+" v"+applicationVersion);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlaceFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
                {
                OpenFileDialog openFileDialog = new OpenFileDialog{};
                
                openFileDialog.Title = "Select a Roblox Place File";
                openFileDialog.Filter = "Roblox Place File (*.rbxl)|*.rbxl|Roblox Place File XML (*.rbxlx)|*.rbxlx";
                openFileDialog.InitialDirectory = Path.GetFullPath(Environment.ProcessPath+@"\..\PlaceFileSamples\");
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog()==true)
                {
                    PlaceFileTextBox.Text = openFileDialog.FileName;
                }
            }
            catch (Exception errMsg)
            {
                MessageBox.Show("Unable to select Place File: Something unexpected happened, Please look at the Error Message below to see if there is a possible fix. If there is no possible fix, please report it to the "+applicationName+" developers.\n\nError Message: "+errMsg, applicationName+" - Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void StartHostButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(PlaceFileTextBox.Text)||!(Path.GetExtension(PlaceFileTextBox.Text)==".rbxl"||Path.GetExtension(PlaceFileTextBox.Text)==".rbxlx"))
                {
                    MessageBox.Show("Please select a valid *.RBXL/*.RBXLX Place File!", applicationName+" - Invalid Place File", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(PortTextBox.Text,out decimal port)||port<1||port>65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)!", applicationName+" - Invalid Port",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }

                port=Math.Round(port,0);

                if (!File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\oldserver.rbxl"))
                {
                    if (File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\server.rbxl"))
                    {
                        File.Move(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\server.rbxl", Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\oldserver.rbxl", false);
                    }
                }
                File.Copy(@PlaceFileTextBox.Text,Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\server.rbxl",true);

                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,applicationName+"SetupLoaderPlugin.rbxmx")))
                {
                    if (File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx"))
                    {
                        File.Delete(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx");
                    }
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,applicationName+"SetupLoaderPlugin.rbxmx"),Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx",true);
                    string setupLoaderPluginContent = File.ReadAllText(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx");
                    if (setupLoaderPluginContent.Contains("--[["+applicationName.ToUpper()+"_SETTINGS_INFORMATION_"+applicationVersion+"]]"))
                    {
                        setupLoaderPluginContent = setupLoaderPluginContent.Replace("--[["+applicationName.ToUpper()+"_SETTINGS_INFORMATION_"+applicationVersion+"]]",$"[\"R6\"]={SettingElement_R6.IsChecked.ToString().ToLower()},[\"ProtectPartNetworkOwnershipChange\"]={SettingElement_ProtectPartNetworkOwnershipChange.IsChecked.ToString().ToLower()},[\"MultipleSamePlayerAllowed\"]={SettingElement_MultipleSamePlayerAllowed.IsChecked.ToString().ToLower()},[\"BXPlayerIDChangeAllowed\"]={SettingElement_BXPlayerIDChangeAllowed.IsChecked.ToString().ToLower()},[\"ServerPlayerPhysics\"]={SettingElement_ServerPlayerPhysics.IsChecked.ToString().ToLower()},[\"ServerPlayerAnimations\"]={SettingElement_ServerPlayerAnimations.IsChecked.ToString().ToLower()},[\"ServerPlayerSounds\"]={SettingElement_ServerPlayerSounds.IsChecked.ToString().ToLower()}");
                        File.WriteAllText(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx", setupLoaderPluginContent);
                    }
                    else
                    {
                        throw new IOException("\""+applicationName+"SetupLoaderPlugin.rbxmx\" does not contain the required settings information placeholder, This means the file is either modified, corrupted, or does not match the application version. To fix this error, re-download "+applicationName+" from the official github page: "+applicationGithubUrl);
                    }
                }
                else
                {
                    throw new FileNotFoundException("\""+applicationName+"SetupLoaderPlugin.rbxmx\" not found in the application directory, This means the file is either modified or corrupted. To fix this error, re-download "+applicationName+" from the official github page: "+applicationGithubUrl);
                }

                string?robloxStudioBeta_path=null;
                foreach (string f in Directory.EnumerateFiles(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions","RobloxStudioBeta.exe", SearchOption.AllDirectories))
                {
                    robloxStudioBeta_path=f;
                    break;
                }

                if (robloxStudioBeta_path==null)
                {
                    throw new FileNotFoundException("Roblox Studio was not found in the Roblox Versions directory, please ensure Roblox Studio is installed correctly.\n\nExpected location: "+Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions\...\RobloxStudioBeta.exe");
                }
            
                var rsbHostServer = Process.Start(new ProcessStartInfo()
                {
                    FileName = robloxStudioBeta_path,
                    Arguments = "-task StartServer -placeId 0 -universeId 0 -placeVersion 0 -port "+port+" -creatorId 1 -creatorType 0 -instanceId Server",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized,
                });

                rsbHostServer.EnableRaisingEvents = true;
                rsbHostServer.Exited+=(s,e)=>
                {
                    if (File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx"))
                    {
                        File.Delete(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx");
                    }
                };
            }
            catch (Exception errMsg)
            {
                MessageBox.Show("Unable to Host Server: Something unexpected happened, Please look at the Error Message below to see if there is a possible fix. If there is no possible fix, please report it to the "+applicationName+" developers.\n\nError Message: "+errMsg, applicationName+" - Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void JoinServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(ServerPortTextBox.Text,out decimal port)||port<1||port>65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)!", applicationName+" - Invalid Port",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }

                string?robloxStudioBeta_path=null;
                foreach (string f in Directory.EnumerateFiles(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions","RobloxStudioBeta.exe", SearchOption.AllDirectories))
                {
                    robloxStudioBeta_path=f;
                    break;
                }

                if (robloxStudioBeta_path==null)
                {
                    throw new FileNotFoundException("Roblox Studio was not found in the Roblox Versions directory, please ensure Roblox Studio is installed correctly.\n\nExpected location: "+Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions\...\RobloxStudioBeta.exe");
                }

                port=Math.Round(port,0);

                var rsbHostServer = Process.Start(new ProcessStartInfo()
                {
                    FileName = robloxStudioBeta_path,
                    Arguments = "-task StartClient -placeId 1818 -universeId 0 -server " + ServerIPAddressTextBox.Text+" -port "+port+" -userid 1 -disableLoadUserPlugins -instanceId Player",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized,
                });
            }
            catch (Exception errMsg)
            {
                MessageBox.Show("Unable to Join Server: Something unexpected happened, Please look at the Error Message below to see if there is a possible fix. If there is no possible fix, please report it to the "+applicationName+" developers.\n\nError Message: "+errMsg, applicationName+" - Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
}

        private void HostServerSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            Title=applicationFullName+" - Host Server";

            HostServerSwitchButton.Background = (new SolidColorBrush(Color.FromRgb(75, 150, 255)));
            JoinServerSwitchButton.Background = (new SolidColorBrush(Color.FromRgb(75, 75, 75)));

            HostServerSection.Visibility = Visibility.Visible;
            JoinServerSection.Visibility = Visibility.Hidden;
        }

        private void JoinServerSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            Title=applicationFullName+" - Join Server";

            HostServerSwitchButton.Background = (new SolidColorBrush(Color.FromRgb(75, 75, 75)));
            JoinServerSwitchButton.Background = (new SolidColorBrush(Color.FromRgb(75, 150, 255)));

            HostServerSection.Visibility = Visibility.Hidden;
            JoinServerSection.Visibility = Visibility.Visible;
        }



        /////////////////////////////////////////////////////////////////////////////



        private void Window_Initialized(object sender, EventArgs e)
        {
            string?robloxStudioBeta_path = null;
            foreach (string f in Directory.EnumerateFiles(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions", "RobloxStudioBeta.exe", SearchOption.AllDirectories))
            {
                robloxStudioBeta_path = f;
                break;
            }
            if (robloxStudioBeta_path == null)
            {
                MessageBox.Show("Roblox Studio has not been detected.\n\nPlease Install Roblox Studio and try again.\n\nlocation check: \""+(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Versions")+"\"\nversion: v"+applicationVersion, applicationFullName+" - Roblox Studio not found.", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            AppName.Content = applicationName;
            AppVersion.Content = "v"+applicationVersion;
            AppDescription.Content = applicationDescription;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HostServerSwitchButton_Click(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\oldserver.rbxl"))
            {
                File.Copy(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\oldserver.rbxl", Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\server.rbxl", true);
                File.Delete(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\oldserver.rbxl");
            }
            if (File.Exists(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx"))
            {
                File.Delete(Environment.GetEnvironmentVariable("localappdata")+@"\Roblox\Plugins\"+applicationName+"SetupLoaderPlugin.rbxmx");
            }
        }
    }
}
