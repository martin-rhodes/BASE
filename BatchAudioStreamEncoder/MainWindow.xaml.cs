using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace BatchAudioStreamEncoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Encoder encoder = new Encoder();
        public Thread encoderThread;

        #region GUI variables

        private ProgressWindow progressWindow;
        string previouslySelectedFolder = string.Empty;
        private string VLCNotFoundString;

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            DataContext = encoder;

            VLCNotFoundString = Properties.Resources.PleaseSetVLCLocationText;

            if (!File.Exists(Properties.Settings.Default.VLCExecutable) || (!Properties.Settings.Default.VLCExecutable.EndsWith("vlc.exe", true, System.Globalization.CultureInfo.InvariantCulture)))
             {
                 Properties.Settings.Default.VLCExecutable = VLCNotFoundString;
                 Properties.Settings.Default.Save();
             }
            
            // if VLC location has not yet been set
            if (Properties.Settings.Default.VLCExecutable == VLCNotFoundString)
            {
                // check 32 bit default location
                if (File.Exists(Properties.Settings.Default.DefaultVLCLocation32))
                {
                    Properties.Settings.Default.VLCExecutable = Properties.Settings.Default.DefaultVLCLocation32;
                    Properties.Settings.Default.Save();
                }
                    // else check 64 bit default location
                else if (File.Exists(Properties.Settings.Default.DefaultVLCLocation64))
                {
                    Properties.Settings.Default.VLCExecutable = Properties.Settings.Default.DefaultVLCLocation64;
                    Properties.Settings.Default.Save();
                }

                    // if default is not found, show error
                else
                {
                    ShowVLCNotFoundError();
                }

            }
        }

        private void ShowVLCNotFoundError()
        {
            System.Windows.MessageBox.Show(Properties.Resources.VLCNotFoundText, Properties.Resources.VLCNotFoundTitle);
        }

        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = previouslySelectedFolder;
            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!encoder.SourceFolderList.Contains(dialog.SelectedPath))
                {
                    encoder.SourceFolderList.Add(dialog.SelectedPath);
                    previouslySelectedFolder = dialog.SelectedPath;
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            encoder.SourceFolderList.Remove((string)FolderListBox.SelectedItem);
        }

        private void btnStartEncoding_Click(object sender, RoutedEventArgs e)
        {

            if (!File.Exists(encoder.VLCExecutable))
            {
                string message = string.Format(Properties.Resources.VLCNotFoundAtText, encoder.VLCExecutable);
                System.Windows.MessageBox.Show(message);
                return;
            }

            if (encoder.OutputFolder.IndexOf(@":\") == -1)
            {
                System.Windows.MessageBox.Show("Please set an output folder before starting the encoding process",
                                "Output folder not set");
                return;
            }

            encoder.CurrentProgressPercentage = 0;
            encoder.CurrentProgressText = string.Empty;

            encoderThread = new Thread(new ThreadStart(encoder.Encode));
            encoderThread.Start();

            progressWindow = new ProgressWindow(encoder, this);
            progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progressWindow.Owner = this;

            btnStartEncoding.IsEnabled = false;

            progressWindow.ShowDialog();
        }

        private void btnSelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = encoder.OutputFolder;
            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                encoder.OutputFolder = dialog.SelectedPath;
            }
        }

        private void BtnSelectVLCExecutable_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VLC Executable|vlc.exe";

            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.FileName.EndsWith("vlc.exe", true, System.Globalization.CultureInfo.InvariantCulture))
                {

                    Properties.Settings.Default.VLCExecutable = dialog.FileName;
                    encoder.VLCExecutable = dialog.FileName;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    System.Windows.MessageBox.Show("The file selected was not vlc.exe", "VLC.exe not selected");
                }
            }
        }

        private void FileMenuExit_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void HelpMenuAbout_OnClick(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            about.ShowDialog();
        }

        private void HelpMenuHelp_OnClick(object sender, RoutedEventArgs e)
        {
            HelpWindow help = new HelpWindow();
            help.Owner = this;
            help.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            help.ShowDialog();
        }
    }
}

