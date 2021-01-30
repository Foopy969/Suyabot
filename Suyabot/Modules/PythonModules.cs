using Discord.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suyabot.Modules
{
    public class PythonModules : ModuleBase<SocketCommandContext>
    {
        [Command("python")]
        [Alias("py", "script")]
        public async Task Python(params string[] args)
        {
            if (args.Length == 0)
            {
                await Help();
                return;
            }

            try
            {
                switch (args[0])
                {
                    case "help":
                        await Help();
                        break;
                    case "list":
                        await List();
                        break;
                    case "cat":
                        await Cat(args[1]);
                        break;
                    case "new":
                        await New(args[1]);
                        break;
                    case "remove":
                        await Remove(args[1]);
                        break;
                    default:
                        await Run(args[0], args.Skip(1).ToArray());
                        break;
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendEmbedAsync("Error", e.Message);
            }
        }

        private async Task Cat(string name)
        {
            if (!Directory.GetFiles(@"python").Any(x => Path.GetFileNameWithoutExtension(x) == name))
            {
                Extensions.Log("Error", $"{name}.py not found");
                await Context.Channel.SendEmbedAsync("Error", $"`{name}.py` not found");
                return;
            }

            if (Regex.Matches(Context.Message.Content, "```").Count != 2)
            {
                await Context.Channel.SendMessageAsync($"`cat {name}.py`\n```py\n{File.ReadAllText($@"python\{name}.py")}\n```");
                return;
            }

            string[] texts = Context.Message.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string text = string.Join("\n", texts.Skip(1).Where(x => !x.Contains("```")));

            File.WriteAllText($@"python\{name}.py", text);
            Extensions.Log("Info", $"{name}.py has been updated");

            await Context.Channel.SendEmbedAsync($"`{name}.py` has been updated");
        }

        private async Task Help()
        {
            await Context.Channel.SendMessageAsync("```\nhelp\nlist\nnew [name] [code]\ncat [name] [code(optional)]\nremove [name]\n```");
        }

        private async Task Run(string name, string[] args)
        {
            if (!Directory.GetFiles(@"python").Any(x => Path.GetFileNameWithoutExtension(x) == name))
            {
                Extensions.Log("Error", $"{name}.py not found");
                await Context.Channel.SendEmbedAsync("Error", $"`{name}.py` not found");
                return;
            }

            Process process = StartProcess($@"{Config.PythonPath}\python.exe", $@"python\{name}.py " + string.Join(" ", args));
            process.Start();

            if (!process.WaitForExit(5000))
            {
                Extensions.Log("Error", $"{name}.py timed out");
                await Context.Channel.SendEmbedAsync("Timeout", $"`{name}.py` not responding after 5 seconds");
            }
            else
            {
                Extensions.Log("Info", $"executing {name}.py");
                if (process.ExitCode == 0)
                {
                    Extensions.Log("Info", $"{name}.py exited with exitcode 0");
                    await Context.Channel.SendMessageAsync(process.StandardOutput.ReadToEnd());
                }
                else
                {
                    Extensions.Log("Info", $"{name}.py exited with exitcode {process.ExitCode}");
                    await Context.Channel.SendEmbedAsync($"Error ({process.ExitCode})", process.StandardError.ReadToEnd());
                }
            }
        }

        private async Task New(string name)
        {
            if (Regex.Matches(Context.Message.Content, "```").Count != 2)
            {
                Extensions.Log("Error", "codeblock can't be read");
                await Context.Channel.SendEmbedAsync("Error", "Codeblock can't be read");
                return;
            }

            if (Directory.GetFiles(@"python").Any(x => Path.GetFileNameWithoutExtension(x) == name))
            {
                Extensions.Log("Error", $"{name}.py already exists");
                await Context.Channel.SendEmbedAsync("Error", $"`{name}.py` already exists");
                return;
            }

            if (name == "new" || name == "help" || name == "remove" || name == "list" || name == "cat")
            {
                await Context.Channel.SendEmbedAsync("Error", $"`{name}` is a reserved command name");
                return;
            }

            string[] texts = Context.Message.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string text = string.Join("\n", texts.Skip(1).Where(x => !x.Contains("```")));

            File.WriteAllText($@"python\{name}.py", text);
            Extensions.Log("Info", $"{name}.py has been created");

            await Context.Channel.SendEmbedAsync($"Command `{name}` has been created");
        }


        private async Task Remove(string name)
        {
            if (!Directory.GetFiles(@"python").Any(x => Path.GetFileNameWithoutExtension(x) == name))
            {
                await Context.Channel.SendEmbedAsync("Error", $"`{name}.py` not found");
                return;
            }

            File.Delete($@"python\{name}.py");
            Extensions.Log("Info", $"{name}.py has been removed");

            await Context.Channel.SendEmbedAsync($"`{name}.py` has been deleted");
        }

        private async Task List()
        {
            if (Directory.GetFiles(@"python").Any())
            {
                await Context.Channel.SendMessageAsync("`list`\n```css\n" + string.Join("\n", Directory.GetFiles(@"python").Select(x => Path.GetFileName(x))) + "\n```");
            }
            else
            {
                await Context.Channel.SendEmbedAsync("List is empty");
            }
        }

        private Process StartProcess(string fileName, string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }
    }
}
