using Newtonsoft.Json;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Suyabot
{
    public static class Config
    {
        public static string ConfigPath = @"json\config.json";
        public static string GuildsPath = @"json\guilds.json";
        public static string ProfilesPath = @"json\profiles.json";

        public static string Token => Reader.Token;
        public static string Prefix => Reader.Prefix;
        public static string PythonPath => Reader.PythonPath;
        public static bool Mention => Reader.Mention;

        public static ConfigReader Reader { get; private set; }
        public static List<Guild> Guilds { get; private set; }
        public static List<Profile> Profiles { get; private set; }

        public static bool Read()
        {
            try
            {
                Extensions.Log("Info", $"Reading Config from {ConfigPath}");
                Reader = JsonConvert.DeserializeObject<ConfigReader>(File.ReadAllText(ConfigPath));
            }
            catch
            {
                Extensions.Log("Error", $"Couldn't read from config, exiting...");
                return false;
            }

            Guilds = TryRead<Guild>(GuildsPath);
            Profiles = TryRead<Profile>(ProfilesPath);

            Extensions.Log("Info", $"Prefix is {Prefix}");

            return true;
        }

        public static List<T> TryRead<T>(string path)
        {
            T[] result = new T[0];
            if (TryDeserialize(File.ReadAllText(path), ref result))
            {
                Extensions.Log("Info", $"Reading {typeof(T).Name}s from {path}");
                return result.ToList();
            }
            else
            {
                Extensions.Log("Debug", $"Couldn't read from {path}, created empty list instead");
                return new List<T>();
            }
        }
        
        public static void Write(string path, object value)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
            }
            catch
            {
                Extensions.Log("Error", $"Failed to write {value} to {path}");
            }
            finally
            {
                Extensions.Log("Info", $"Written {value} to {path}");
            }
        }

        public static bool GetGuildChannel(ulong serverID, ref ulong channelID)
        {
            if (!GuildExists(serverID)) return false;
            Guild guild = Guilds.Where(x => x.ServerID == serverID).FirstOrDefault();
            channelID = guild.ChannelID;
            return guild.State;
        }

        public static bool GuildExists(ulong serverID)
        {
            return Guilds.Any(x => x.ServerID == serverID);
        }

        public static bool TryDeserialize<T>(string value, ref T result)
        {
            try
            {
                result = JsonConvert.DeserializeObject<T>(value);
            }
            catch
            {
                Extensions.Log("Error", $"Failed to deserialize {typeof(T)}");
                return false;
            }
            return true;
        }
    }

    public class ConfigReader
    {
        public string Token;
        public string Prefix;
        public string PythonPath;
        public bool Mention;
    }

    public class Guild
    {
        public ulong ServerID;
        public ulong ChannelID;
        public bool State;

        public Guild(ulong serverID, ulong channelID, bool state)
        {
            ServerID = serverID;
            ChannelID = channelID;
            State = state;
        }
    }

    public class Profile
    {
        public int Level { get; set; }
        public int Credit { get; set; }
        public int Exp { get; set; }
        public ulong UserID { get; set; }

        public Profile(ulong _UserID)
        {
            UserID = _UserID;
            Level = 0;
            Credit = 10;
            Exp = 0;
        }

        public int GetMaxExp()
        {
            return 25 * Level * (1 + Level);
        }

        public string GetExpBar()
        {
            int progress = 10 * Exp / GetMaxExp();
            return new string('█', progress) + new string('░', 10 - progress);
        }

        public bool Add(int exp, int credit)
        {
            Exp += exp;
            Credit += credit;

            if (Exp > GetMaxExp())
            {
                LevelUp();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void LevelUp()
        {
            Exp -= GetMaxExp();
            Credit += 50 * Level;
            Level++;
        }
    }
}
