using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Robertof.PushBulletAPI;
using System;
using System.Xml.Serialization;

namespace PushBullet
{
    public partial class APIGUI2 : Window
    {
        public APIGUI2()
        {
            InitializeComponent();
            if (PushBulletAPI.HasConfigurationOption(PushBullet.conf, "apikey"))
                this.apikeyInput.Text = PushBulletAPI.GetNonNullConfigurationOption(PushBullet.conf, "apikey");
            this.doneWindowChkBox.IsChecked = PushBulletAPI.GetBoolConfigurationOption(PushBullet.conf, "showDoneWindow", true);
            this.Loaded += WinLoaded;
        }

        private void PressEvent()
        {
            MessageBox.Show("Global hotkey pressed");
        }

        private void OnSaveBtnClicked(object sender, RoutedEventArgs e)
        {
            var key = apikeyInput.Text;
            /* Removed this check since I'm unsure if PushBullet's APIkey
             * is always 32 characters. Since it is an MD5 hash encoded with
             * Base64, I think that Base64's padding may alter its length,
             * so better leave this commented.
             * if (key.Length != 32)
                PushBullet.ShowError(Properties.Strings.InvalidKey);
            else
            {*/
            this.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            PushBulletAPI.Configure(key);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += CheckAPIKeyBg;
            bw.RunWorkerCompleted += UpdateUIElements;
            bw.RunWorkerAsync();
            //}
        }

        private void OnUndoBtnClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CheckAPIKeyBg (object sender, DoWorkEventArgs e)
        {
            PushBulletAPI.DevicesResponse devices;
            try
            {
                devices = PushBulletAPI.GetDevices();
            }
            catch (Exception ex)
            {
                e.Result = false;
                PushBullet.ShowError(Properties.Strings.InvalidKey + " " + ex.ToString());
                return;
            }
            e.Result = devices;
        }

        private void UpdateUIElements(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is PushBulletAPI.DevicesResponse)
            {
                var res = e.Result as PushBulletAPI.DevicesResponse;
                PushBulletAPI.SetConfigurationOption(PushBullet.conf, "apikey", apikeyInput.Text, false);
                if (doneWindowChkBox.IsChecked.HasValue)
                    PushBulletAPI.SetConfigurationOption(PushBullet.conf, "showDoneWindow", (bool) doneWindowChkBox.IsChecked, false);
                var section = PushBulletAPI.GetResponseSection(PushBullet.conf);
                section.Devices.Clear();
                section.SharedDevices.Clear();
                foreach (PushBulletAPI.DevicesResponse.Device device in res.devices)
                    section.Devices.Add(device.ToDeviceConfig());
                foreach (PushBulletAPI.DevicesResponse.SharedDevice sDevice in res.shared_devices)
                    section.SharedDevices.Add(sDevice.ToDeviceConfig());
                PushBullet.conf.Save(System.Configuration.ConfigurationSaveMode.Modified);
                MessageBox.Show(Properties.Strings.SetupComplete, @"\o/", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }
            this.IsEnabled = true;
            Mouse.OverrideCursor = null;
            apikeyInput.Clear();
        }

        private void onTextboxFocus(object sender, MouseButtonEventArgs e)
        {
            if (apikeyInput.Text.Equals("API key..."))
                apikeyInput.Clear();
            else
                apikeyInput.SelectAll();
        }

        private void WinLoaded(object sender, RoutedEventArgs e)
        {
            int n = HotkeyManager.Instance.RegisterHotkey(HotkeyManager.Modifiers.Ctrl | HotkeyManager.Modifiers.Shift, HotkeyManager.Keys.A, this, PressEvent);
            MessageBox.Show("Hotkey registered " + n);
        }


    }
}
