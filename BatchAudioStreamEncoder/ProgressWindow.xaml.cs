using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Shell;

namespace BatchAudioStreamEncoder
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private Encoder encoder;
        private MainWindow parent;

        public ProgressWindow(Encoder encoder, MainWindow parent)
        {
            InitializeComponent();

            this.encoder = encoder;
            this.parent = parent;

            DataContext = encoder;

            taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void Button_Click_Abort(object sender, RoutedEventArgs e)
        {

            encoder.IsProcessAborted = true;

            if (((encoder.Process != null) && !(encoder.Process.HasExited)))
            {
                encoder.Process.Kill();
            }

            parent.btnStartEncoding.IsEnabled = true;

            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            encoder.IsProcessAborted = true;

            if (((encoder.Process != null) && !(encoder.Process.HasExited)))
            {
                encoder.Process.Kill();
            }

            parent.btnStartEncoding.IsEnabled = true;
        }

    }
}
