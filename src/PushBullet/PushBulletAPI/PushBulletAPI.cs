using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Robertof.PushBulletAPI
{
    public class PushBulletAPI
    {
        public delegate bool DataUploadedListener (long uploaded, long totalSize);
        private static String key;

        public static void Configure(string apikey)
        {
            key = apikey;
        }

        public static DevicesResponse GetDevices()
        {
            string res;
            try
            {
                res = PerformHTTPRequest("https://www.pushbullet.com/api/devices");
            }
            catch (Exception e) { throw e; }
            var devices = JsonConvert.DeserializeObject<DevicesResponse>(res);
            CheckForErrors(devices);
            return devices;
        }

        public static PushResponse PushFile(string target, string path, DataUploadedListener listener)
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("device_id", target);
            data.Add("type", "file");
            var res = HttpUploadFile("https://www.pushbullet.com/api/pushes", path, "file", "application/octet-stream", data, listener);
            if (res == null || res.Length == 0) throw new Exception("Got null from our request :(");
            var finalized = JsonConvert.DeserializeObject<PushResponse>(res);
            CheckForErrors(finalized);
            return finalized;
        }

        private static void CheckForErrors(ErrorableResponse res)
        {
            if (res == null || (res.type != null && res.type.Length > 0))
                throw new Exception("Got an invalid response." + ((res.type != null && res.type.Length > 0) ? " Error: " + res.type : ""));
        }

        private static string PerformHTTPRequest(string url)
        {
            var _cl = WebRequest.Create(url);
            _cl.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(key + ":"));
            HttpWebResponse sres = (HttpWebResponse) _cl.GetResponse();
            if (sres.StatusCode != HttpStatusCode.OK)
                throw new Exception("Server sent a bad code: " + sres.StatusCode);
            string s;
            using (StreamReader reader = new StreamReader(sres.GetResponseStream()))
                s = reader.ReadToEnd();
            sres.Close();
            if (s == null || s.Length == 0)
                throw new Exception("Cannot read data from the stream (empty output)");
            return s;
        }

        // performs an HTTP multipart request. Thanks to stackoverflow
        // http://stackoverflow.com/a/2996904 (slightly modified by adding a
        // data sent callback, chunked tranfer and authentication)
        private static string HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc, DataUploadedListener listener)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest) WebRequest.Create(url);
            wr.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(key + ":"));
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.AllowWriteStreamBuffering = false;
            wr.SendChunked = true;
            //wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string k in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, k, nvc[k]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, Path.GetFileName (file), contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            long totalBytes = fileStream.Length; long sentBytes = 0;
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
                sentBytes += bytesRead;
                if (listener != null)
                    if (!listener(sentBytes, totalBytes))
                        throw new System.Threading.ThreadInterruptedException();
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                return reader2.ReadToEnd();
            }
            catch (Exception ex)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
                throw ex;
            }
            finally
            {
                wr = null;
            }
        }

        // utility functions for configuration files
        // opens a configuration file from %APPDATA%\{baseName}.config
        public static Configuration GetSharedConfiguration(string baseName)
        {
            string completePath = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), baseName + ".config");
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = completePath;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            if (config.Sections["response"] == null)
                config.Sections.Add("response", new ResponseSection());
            return config;
        }

        // sets a config option from conf, save should be true if you want to save instantly
        public static void SetConfigurationOption(Configuration conf, string key, string value, bool save)
        {
            if (conf.AppSettings.Settings[key] != null)
                conf.AppSettings.Settings[key].Value = value;
            else
                conf.AppSettings.Settings.Add(key, value);
            if (save)
                conf.Save(ConfigurationSaveMode.Modified);
        }

        // returns a non-null value
        public static string GetNonNullConfigurationOption(Configuration conf, string key)
        {
            KeyValueConfigurationElement elm = conf.AppSettings.Settings[key];
            if (elm == null) return "";
            return elm.Value;
        }

        // true if {key} exists in {conf}
        public static bool HasConfigurationOption(Configuration conf, string key)
        {
            return conf.AppSettings.Settings[key] != null;
        }

        public static bool GetBoolConfigurationOption(Configuration conf, string key, bool defaultVal)
        {
            if (conf.AppSettings.Settings[key] == null) return defaultVal;
            return conf.AppSettings.Settings[key].Equals("true");
        }

        public static void SetConfigurationOption(Configuration conf, string key, bool val, bool save)
        {
            SetConfigurationOption(conf, key, val.ToString().ToLowerInvariant(), save);
        }

        public static ResponseSection GetResponseSection(Configuration conf)
        {
            return (ResponseSection) conf.GetSection("response");
        }

        public class DeviceConfig : ConfigurationElement
        {
            public DeviceConfig() { }
            public DeviceConfig(int id, string model, string nickname, string owner)
            {
                Id = id;
                Model = model;
                Nickname = nickname;
                Owner = owner;
            }

            public DeviceConfig(int id, string model, string nickname) : this(id, model, nickname, null)
            {
            }

            [ConfigurationProperty("id", IsRequired = true, IsKey = true)]
            public int Id
            {
                get
                {
                    return (int) this["id"];
                }
                set
                {
                    this["id"] = value;
                }
            }

            [ConfigurationProperty("model", IsRequired = true, IsKey = true)]
            public string Model
            {
                get
                {
                    return (string) this["model"];
                }
                set
                {
                    this["model"] = value;
                }
            }

            [ConfigurationProperty("nickname", IsRequired = false, IsKey = true)]
            public string Nickname
            {
                get
                {
                    return (string) this["nickname"];
                }
                set
                {
                    this["nickname"] = value;
                }
            }

            [ConfigurationProperty("owner", IsRequired = false, IsKey = true)]
            public string Owner
            {
                get
                {
                    return (string) this["owner"];
                }
                set
                {
                    this["owner"] = value;
                }
            }
        }

        public class DeviceCollection : ConfigurationElementCollection
        {
            public DeviceConfig this[int index]
            {
                get
                {
                    try
                    {
                        return (DeviceConfig) BaseGet(index);
                    }
                    catch(Exception)
                    {
                        return null;
                    }
                }
                set
                {
                    if (BaseGet(index) != null)
                        BaseRemoveAt(index);
                    BaseAdd(index, value);
                }
            }

            public void Add(DeviceConfig conf)
            {
                BaseAdd(conf);
            }

            public void Clear()
            {
                BaseClear();
            }

            protected override ConfigurationElement CreateNewElement()
            {
                return new DeviceConfig();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return ((DeviceConfig) element).Id;
            }

            public void Remove(DeviceConfig conf)
            {
                BaseRemove(conf.Id);
            }

            public void RemoveAt(int index)
            {
                BaseRemoveAt(index);
            }

            public void Remove(string name)
            {
                BaseRemove(name);
            }
        }

        public sealed class ResponseSection : ConfigurationSection
        {
            public ResponseSection() { }

            [ConfigurationProperty("devices", IsDefaultCollection = false)]
            [ConfigurationCollection(typeof(DeviceCollection), AddItemName = "device")]
            public DeviceCollection Devices
            {
                get
                {
                    return (DeviceCollection) base["devices"];
                }
            }

            [ConfigurationProperty("sharedDevices", IsDefaultCollection = false)]
            [ConfigurationCollection(typeof(DeviceCollection), AddItemName = "sharedDevice")]
            public DeviceCollection SharedDevices
            {
                get
                {
                    return (DeviceCollection) base["sharedDevices"];
                }
            }
        }

        // response sent by /api/devices
        public class DevicesResponse : ErrorableResponse
        {
            public Device[] devices { get; set; }
            public SharedDevice[] shared_devices { get; set; }
            public class DeviceExtras
            {
                public string manufacturer { get; set; }
                public string model { get; set; }
                public string android_version { get; set; }
                public string sdk_version { get; set; }
                public string app_version { get; set; }
                public string nickname { get; set; }
            }

            public class Device
            {
                public int id { get; set; }
                public DeviceExtras extras { get; set; }

                public virtual DeviceConfig ToDeviceConfig()
                {
                    return new DeviceConfig(id, extras.model, extras.nickname);
                }
            }

            public class SharedDevice : Device
            {
                public string owner_name { get; set; }

                public override DeviceConfig ToDeviceConfig()
                {
                    return new DeviceConfig(id, extras.model, extras.nickname, owner_name);
                }
            }
        }

        // response sent by /api/pushes
        public class PushResponse : ErrorableResponse
        {
            public long created { get; set; }
            public PushResponseData data { get; set; }
            public int device_id { get; set; }
            public int id { get; set; }
            public long modified { get; set; }
            public int state { get; set; }

            public class PushResponseData
            {
                public string account { get; set; }
                // for files
                public string file_id { get; set; }
                public string file_name { get; set; }
                public string file_type { get; set; }
                // for notes
                public string title { get; set; }
                public string body { get; set; }
                // for links
                // title: already defined above
                public string url { get; set; }
                // for addresses
                public string name { get; set; }
                public string address { get; set; }
                // for lists
                // title: already defined above
                // items: not sure if array or plain string, TODO
                public string install_id { get; set; }
                public string type { get; set; }
            }
        }

        public class ErrorableResponse
        {
            public string type { get; set; }
        }
    }
}
