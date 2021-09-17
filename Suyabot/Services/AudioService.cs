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
        static Song current;
        static List<Song> songs = new List<Song>();

        static ISocketMessageChannel channel;
        static IAudioClient client;
        static AudioOutStream stream;
        static Stream song;

        public static bool IsBound => channel != null;
        public static bool IsPlaying => current != null;
        public static List<Song> GetSongs => songs.Prepend(current).ToList();

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
                        songs.Add(new Song(obj));
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
            try
            {
                current = null;
                song.Close();
                song.Dispose();
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

        public static void Initialize(IAudioClient client, ISocketMessageChannel channel)
        {
            AudioService.client = client;
            AudioService.channel = channel;

            stream = client.CreatePCMStream(AudioApplication.Mixed);
        }

        public static void Dispose()
        {
            current = null;
            channel = null;
            stream.Close();
            stream.Dispose();
            client.StopAsync();
        }

        public static async Task PlayAsync()
        {
            while (songs.Any())
            {
                await channel.SendMessageAsync($"**Playing** :notes: `{songs[0].Title}` - Now!");

                using (song = CreateProcess("ffmpeg.exe", $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -hide_banner -loglevel error -i \"{songs[0].Audio}\" -ac 2 -f s16le -ar 48000 pipe:1").StandardOutput.BaseStream)
                {
                    current = songs[0];
                    songs.RemoveAt(0);

                    try { await song.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }
                }
            }

            current = null;
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
            Audio = (string)json["formats"].Where(x => x["protocol"].ToString().Contains("http") && x["format"].ToString().Contains("audio only")).Last()["url"];
            Thumbnail = (string)json["thumbnail"];
            Duration = (int)json["duration"];
        }

        public int TimeLeft(DateTime start)
        {
            return Duration - Convert.ToInt32((DateTime.Now - start).TotalSeconds);
        }

        public override string ToString()
        {
            return $"[{Title}]({Url}) | `{Duration / 60}:{Duration % 60}`\n";
        }
    }
}
