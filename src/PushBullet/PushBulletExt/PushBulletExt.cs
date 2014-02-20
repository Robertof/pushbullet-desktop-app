using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Robertof.PushBulletAPI;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace PushBulletExt
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class PushBulletExt : SharpContextMenu
    {
        private PushBulletAPI.ResponseSection devices;
        private System.Configuration.Configuration conf;
        private Dictionary<string, Dictionary<string, string>> LOCALIZATION = new Dictionary<string, Dictionary<string, string>>()
        {
            { "default", new Dictionary<string, string>() {
                { "NoDevices", "No devices" },
                { "SendToPushbullet", "Send to PushBullet" },
                { "SharedDevices", "Shared devices" },
                { "Error", "Error" }
            } },
            /*{ "it-IT", new Dictionary<string, string>() {
                { "NoDevices", "Nessun dispositivo" },
                { "SendToPushbullet", "Invia a PushBullet" },
                { "SharedDevices", "Dispositivi condivisi" },
                { "Error", "Errore" }
            } }*/
        };
        private string CULTURE = System.Globalization.CultureInfo.CurrentCulture.ToString();

        protected override bool CanShowMenu()
        {
            //if (!InitDevices()) return false;
            //return this.devices.devices.Length > 0 || this.devices.shared_devices.Length > 0;
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();
            var sendToPushbullet = new ToolStripMenuItem
            {
                Text = GetLocalizedStr("SendToPushbullet"),
                Image = Properties.Resources.pb_ubersmall
            };
            try
            {
                if (!InitDevices())
                    return null;
                if (devices.Devices.Count == 0 && devices.SharedDevices.Count == 0)
                {
                    sendToPushbullet.DropDownItems.Add(GetLocalizedStr("NoDevices")).Enabled = false;
                    menu.Items.Add(sendToPushbullet);
                    return menu;
                }
                if (devices.Devices.Count > 0)
                {
                    //sendToPushbullet.DropDownItems.Add("Your devices").Enabled = false;
                    //sendToPushbullet.DropDownItems.Add("-").Enabled = false;
                    for (int i = 0; i < devices.Devices.Count; i++)
                        sendToPushbullet.DropDownItems.Add(GetDeviceName(i, false), null, handleClick).Tag = devices.Devices[i].Id;
                }
                if (devices.SharedDevices.Count > 0)
                {
                    if (devices.Devices.Count > 0)
                        sendToPushbullet.DropDownItems.Add("-").Enabled = false;
                    sendToPushbullet.DropDownItems.Add(GetLocalizedStr("SharedDevices")).Enabled = false;
                    //sendToPushbullet.DropDownItems.Add("-").Enabled = false;
                    for (int i = 0; i < devices.SharedDevices.Count; i++)
                        sendToPushbullet.DropDownItems.Add(GetDeviceName(i, true), null, handleClick).Tag = devices.SharedDevices[i].Id;
                }
                menu.Items.Add(sendToPushbullet);
                return menu;
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            return null;
        }

        private void handleClick(object sender, EventArgs e)
        {
            //int id = (int) (sender as ToolStripMenuItem).Tag;
            TryExecute(((int) (sender as ToolStripItem).Tag).ToString());
        }

        private string GetDeviceName(int index, bool shared)
        {
            PushBulletAPI.DeviceCollection refs;
            if (shared) refs = this.devices.SharedDevices;
            else refs = this.devices.Devices;
            return (refs[index].Nickname != null ? refs[index].Nickname : refs[index].Model) + (shared ? " (" + refs[index].Owner + ")" : "");
        }

        private bool InitDevices()
        {
            if (devices != null) return true;
            
            try
            {
                if (conf == null) conf = PushBulletAPI.GetSharedConfiguration("pushbullet");
                var data = PushBulletAPI.GetResponseSection(this.conf);
                if (data == null) throw new Exception("Empty ResponseSection");
                this.devices = data;
                // avoid NPEs
                //if (this.devices.Devices.Count == null) this.devices.devices = new PushBulletAPI.DevicesResponse.Device[] { };
                //if (this.devices.shared_devices == null) this.devices.shared_devices = new PushBulletAPI.DevicesResponse.SharedDevice[] { };
                if (devices.SharedDevices.Count == 0) MessageBox.Show("Kaboom");
                return true;
            }
            catch (Exception e)
            {
                err("Got an Exception while retrieving the devices: " + e.ToString());
                return false;
            }
        }

        /*private PushBulletAPI.DeviceCollection GetDevices()
        {
            if (conf == null)
                conf = PushBulletAPI.GetSharedConfiguration("pushbullet");
            if (!PushBulletAPI.HasConfigurationOption(conf, "apikey")) throw new Exception("No device cache available");
            //var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PushBulletAPI.DevicesResponse));
            //return serializer.Deserialize(new System.IO.StringReader(PushBulletAPI.GetNonNullConfigurationOption(conf, "cachedDevices"))) as PushBulletAPI.DevicesResponse;

        }*/

        private void TryExecute(string devId)
        {
            if (conf == null)
                conf = PushBulletAPI.GetSharedConfiguration("pushbullet");
            if (!PushBulletAPI.HasConfigurationOption(conf, "mainPath")) throw new Exception("No executable path available");
            try
            {
                Process.Start(PushBulletAPI.GetNonNullConfigurationOption(this.conf, "mainPath"), "/upload " + devId + " " + MergePaths(this.SelectedItemPaths));
            }
            catch (Exception exa)
            {
                MessageBox.Show("An error occurred while opening the upload helper: " + exa.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string MergePaths(System.Collections.Generic.IEnumerable<string> pathnames)
        {
            string final = "";
            foreach (string val in pathnames)
                final += "\"" + val + "\" ";
            return final;
        }

        private void err(string val)
        {
            MessageBox.Show(val, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private String GetLocalizedStr(string key)
        {
            if (this.LOCALIZATION.ContainsKey(CULTURE))
                return this.LOCALIZATION[CULTURE][key];
            return this.LOCALIZATION["default"][key];
        }

        /*private string GetExecutablePath()
        {
            string s = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\PushBullet_is1", "InstallLocation", "E_NOTFOUND") as string;
            return (s == null || s.Equals ("E_NOTFOUND") || FORCE_DEBUG) ? DEBUG_PATH : s;
        }

        private string WhatDoesTheHelperSay(string param)
        {
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetExecutablePath(),
                    Arguments = param,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode != 0) throw new Exception();//"Exit code != 0");
                string ret = "";
                while (!proc.StandardOutput.EndOfStream)
                    ret += proc.StandardOutput.ReadLine();
                return ret;
            }
            catch
            {
                //MessageBox.Show(ignored.ToString());
                return "";
            }
        }
         */
    }
}
