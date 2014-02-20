using System;
using System.Configuration;
using Robertof.PushBulletAPI;

namespace PushBulletCLI
{
    class PushBulletCLI
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 1) PrintUsage();
            Configuration conf = PushBulletAPI.GetSharedConfiguration("pushbullet");
            switch (args[0].Substring(1))
            {
                case "apikey":
                    if (args.Length < 2 || args[1].Length != 32) PrintUsage();
                    UpdateDevicesCache(conf, args[1], true);
                    break;
                case "devices":
                    if (!PushBulletAPI.HasConfigurationOption(conf, "cachedDevices"))
                    {
                        Console.Error.WriteLine("No device cache available.");
                        System.Environment.Exit(1);
                    }
                    Console.Write(PushBulletAPI.GetNonNullConfigurationOption(conf, "cachedDevices"));
                    break;
                case "refresh":
                    if (!PushBulletAPI.HasConfigurationOption(conf, "apikey"))
                    {
                        Console.Error.WriteLine("API key not set.");
                        System.Environment.Exit(1);
                    }
                    UpdateDevicesCache(conf, PushBulletAPI.GetNonNullConfigurationOption(conf, "apikey"), false);
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }

        private static void UpdateDevicesCache(Configuration conf, string apikey, bool updateKey)
        {
            PushBulletAPI.Configure(apikey);
            try
            {
                var res = PushBulletAPI.GetDevices();
                if (updateKey)
                    PushBulletAPI.SetConfigurationOption(conf, "apikey", apikey, false);
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PushBulletAPI.DevicesResponse));
                using (var writer = new System.IO.StringWriter())
                {
                    serializer.Serialize(writer, res);
                    PushBulletAPI.SetConfigurationOption(conf, "cachedDevices", writer.ToString(), true);
                }
                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Invalid API key.");
                Console.Error.Write(e.ToString());
                System.Environment.Exit(1);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("PushBulletCLI.exe -- provides a convenient CLI interface to PushBullet");
            Console.WriteLine("Usage:");
            Console.WriteLine("/apikey abcd -- sets the API key which will be used for next calls to the API. Will be tested. (implicitly updates devices cache)");
            Console.WriteLine("/devices     -- prints avail. devices in XML from the global cache (in %APPDATA%\\pushbullet.config). Will die if it does not exist.");
            Console.WriteLine("/refresh     -- updates the device cache. The API key must be valid.");
            System.Environment.Exit(0);
        }
    }
}
