using System;
using System.Configuration;
using Robertof.PushBulletAPI;
using System.Linq;

namespace PushBullet
{
    /*
     * Ah, Visual Studio. Why don't you let me
     * use my code style? #@+_!
     * 'kay, back to the serious stuff...
     * PushBullet desktop application.
     * Written by Robertof. Thanks to Ryan!
     */
    public class PushBullet
    {
        public static Configuration conf;

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            conf = PushBulletAPI.GetSharedConfiguration("pushbullet");
            PushBulletAPI.SetConfigurationOption(conf, "mainPath", System.Reflection.Assembly.GetEntryAssembly().Location, true);
            if (args != null && args.Length > 2 && args[0].Equals("/upload"))
            {
                // enter upload mode
                if (!PushBulletAPI.HasConfigurationOption(conf, "apikey") || !System.Text.RegularExpressions.Regex.IsMatch(args[1], @"^\d+$"))
                {
                    ShowError(Properties.Strings.InvalidParams);
                    return;
                }
                new System.Windows.Application().Run(new UploadGUI(args[1], args.Skip(2).ToArray()));
            }
            else
                new System.Windows.Application().Run(new APIGUI2());
        }

        public static void ShowError(string str)
        {
            System.Windows.MessageBox.Show(str, Properties.Strings.Error, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
