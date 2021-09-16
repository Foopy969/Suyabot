using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Suyabot.Services
{
    public static class AudioService
    {
        public static ISocketMessageChannel MessageChannel;
        public static IVoiceChannel VoiceChannel;
        public static IAudioClient Client;
        public static Song Current;

        static DateTime start;
        static List<Song> songs = new List<Song>();
        static Stream stream;

        public static bool IsStreaming => stream != null;
        public static List<Song> GetSongs => songs;

        static Process CreateProcess(string fileName, string arguments) => Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false, RedirectStandardOutput = true });

        public static bool Enqueue(string url, out string errormsg)
        {
            try
            {
                using (Process process = CreateProcess("youtube-dl.exe", $"--quiet -i --skip-download -j {url}"))
                {
                    JObject obj = JObject.Parse(process.StandardOutput.ReadToEnd());

                    if ((int)obj["duration"] < 601)
                    {
                        songs.Add(new Song(obj)); //lowest quality
                    }
                    else throw new FileLoadException("Song longer than 10 mins");
                }

            }
            catch (Exception e)
            {
                errormsg = e.Message;
                return false;
            }
            finally
            {
                errormsg = "Success";
            }

            return true;
        }

        public static bool Dequeue(int index, out string errormsg)
        {
            try
            {
                errormsg = songs[index].Title;
                songs.RemoveAt(index);
            }
            catch (Exception e)
            {
                errormsg = e.Message;
                return false;
            }

            return true;
        }

        public static bool Skip(int count, out string errormsg)
        {
            if (stream == null)
            {
                errormsg = "Nothing is playing";
                return false;
            }
            else
            {
                if (count > 0 && songs.Count() >= count)
                {
                    songs.RemoveRange(0, count);
                }

                errormsg = "Success";
                stream.Close();
                return true;
            }
        }

        public static async Task PlayAsync(IAudioClient client, ISocketMessageChannel channel)
        {
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                while (songs.Any())
                {
                    await channel.SendMessageAsync($"**Playing** :notes: `{songs[0].Title}` - Now!");

                    using (stream = CreateProcess("ffmpeg.exe", $"-hide_banner -loglevel error -i \"{songs[0].Audio}\" -ac 2 -f s16le -ar 48000 pipe:1").StandardOutput.BaseStream)
                    {
                        start = DateTime.Now;
                        Current = songs[0];
                        songs.RemoveAt(0);

                        try { await stream.CopyToAsync(discord); }
                        finally { await discord.FlushAsync(); }
                    }
                }
            }
        }
    }

    public class Song
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Artist { get; set; }
        public string Audio { get; set; }
        public string Thumbnail { get; set; }
        public int Duration { get; set; } //in seconds

        public Song (JObject json)
        {
            Title = (string)json["title"];
            Url = (string)json["webpage_url"];
            Artist = (string)json["uploader"];
            Audio = (string)json["formats"].Where(x => x["protocol"].ToString().Contains("http") && x["format"].ToString().Contains("audio only")).First()["url"]; //lowest quality
            Thumbnail = (string)json["thumbnail"];
            Duration = (int)json["duration"];
        }

        public override string ToString()
        {
            return $"[{Title}]({Url}) | `{Duration / 60}:{Duration % 60}`";
        }

    }
}
