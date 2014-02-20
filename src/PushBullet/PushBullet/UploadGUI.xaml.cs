using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Windows;
using System.ComponentModel;
using Robertof.PushBulletAPI;

namespace PushBullet
{
    public partial class UploadGUI : Window
    {
        private string deviceId;
        private string[] files;
        private BackgroundWorker currentWorker;
        private int i = -1;

        public UploadGUI(string deviceId, string[] files)
        {
            InitializeComponent();
            this.deviceId = deviceId;
            this.files = files;
            PushBulletAPI.Configure(PushBulletAPI.GetNonNullConfigurationOption(PushBullet.conf, "apikey"));
            ProcessFile();
        }

        private void ProcessFile()
        {
            i++;
            if (files.Length <= i || files[i] == null)
            {
                if (PushBulletAPI.GetBoolConfigurationOption(PushBullet.conf, "showDoneWindow", true))
                    MessageBox.Show(Properties.Strings.UploadCompleted, "Ok", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Dispatcher.BeginInvoke(new Action(() => Application.Current.Shutdown()));
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() => uploadLabel.Content = string.Format(Properties.Strings.UploadingFormatStr, Path.GetFileName(files[i]), i + 1, files.Length)));
            if (!File.Exists(files[i]))
            {
                PushBullet.ShowError(string.Format(Properties.Strings.FileDoesNotExist, files[i]));
                ProcessFile();
                return;
            }
            FileInfo info = new FileInfo(files[i]);
            if (info.Length > 0x1900000 || info.Length == 0) // 0x1900000 == 26214400 bytes == 25 MiB
            {
                PushBullet.ShowError(string.Format(Properties.Strings.IncorrectSize, files[i]));
                ProcessFile();
                return;
            }
            currentWorker = new BackgroundWorker();
            currentWorker.DoWork += RunInBackground;
            currentWorker.ProgressChanged += UpdateProgress;
            currentWorker.RunWorkerCompleted += UpdateUIElements;
            currentWorker.WorkerReportsProgress = true;
            currentWorker.WorkerSupportsCancellation = true;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => currentWorker.RunWorkerAsync(new string[] { deviceId, files[i] })));
        }

        private void RunInBackground(object sender, DoWorkEventArgs e)
        {
            string[] arguments = (string[]) e.Argument;
            try
            {
                PushBulletAPI.PushFile(arguments[0], arguments[1], OnDataUploaded);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof (System.Threading.ThreadInterruptedException))
                    PushBullet.ShowError(string.Format (Properties.Strings.UploadError, ex.ToString()));
                else
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => Application.Current.Shutdown()));
            }
        }

        private void UpdateUIElements(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => progressBar1.Value = 0));
            ProcessFile();
        }

        private void UpdateProgress(object sender, ProgressChangedEventArgs e)
        {
            //progressBar1.Value = e.ProgressPercentage;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => progressBar1.Value = e.ProgressPercentage));
        }

        private bool OnDataUploaded(long uploaded, long totalSize)
        {
            currentWorker.ReportProgress((int)((uploaded * 100) / totalSize));
            //System.Diagnostics.Trace.WriteLine((int) ((uploaded * 100) / totalSize));
            return !currentWorker.CancellationPending;
        }

        private void stahp(object sender, RoutedEventArgs e)
        {
            if (!currentWorker.CancellationPending)
                currentWorker.CancelAsync();
        }
    }
}
