using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;

namespace DiscordTestBot {
    public class HelpModule : ModuleBase {
        
        public CommandService CommandService { get; set; }
        public LoggingService Logging { get; set; }
        public IConfigurationRoot Configuration { get; set; }

        [Command("help"), Summary("Displays a help message for commands.")]
        [UsageExample("help testmod echo", "Displays help for the command `echo` from the `testmod` module.")]
        public async Task Help([Remainder] string path = null) {
            // delete original message, if possible.
            if (!(Context.Channel is IDMChannel))
                await Context.Message.DeleteAsync();
            
            if (path == null) {
                await ShowGeneralHelp();
            } else {
                // try to find the command
                var searchResult = CommandService.Search(Context, path);
                if (searchResult.IsSuccess) {
                    await ShowCommandHelp(searchResult.Commands.Select(r => r.Command));
                } else {
                    // otherwise try to find a module
                    var pathArray = path.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    var module = NavigatePath(pathArray);
                    if (module != null) {
                        await ShowModuleHelp(module);
                    } else {
                        await Context.User.SendMessageAsync($"Failed to find anything at path {path}.\nSend `{Configuration["prefix"]}help` to see instructions.");
                    }
                }
            }
        }

        private async Task ShowGeneralHelp() {
            var b = new EmbedBuilder();

            string prefix = Configuration["prefix"];
            string usage = $"Send commands to the bot by prefixing them with `{prefix}`. "+
                           $"For example, `{prefix} help echo` will show help for the `echo` command. " + 
                           $"Use `{prefix}help help` for additional information about the `help` command. " +
                           $"For commands that have multiple words in them like `foo bar`, you can also query for any prefix, e.g. `{prefix}help foo` will show help about the `foo` module.";
            b.WithTitle("Help")
             .WithColor(Constants.DiscordBlue)
             .WithDescription(usage);
            var commands = await GetAvailableCommands();
            b.AddField("Available Commands", FormatCommandList(commands));
            await Context.User.SendMessageAsync(string.Empty, embed: b.Build());
        }

        private async Task ShowCommandHelp(IEnumerable<CommandInfo> commands) {
            foreach (var cmd in commands) {
                var checkResult = await cmd.CheckPreconditionsAsync(Context);
                if (checkResult.IsSuccess) {
                    await Context.User.SendMessageAsync(string.Empty, embed: MakeCommandEmbed(cmd));
                }
            }
        }

        private async Task ShowModuleHelp(ModuleInfo info) {
            var b = new EmbedBuilder();
            b.WithTitle("Module " + info.Aliases[0])
             .WithColor(Constants.DiscordBlue);
            if (!string.IsNullOrWhiteSpace(info.Summary))
                b.WithDescription(info.Summary);
            if (!string.IsNullOrWhiteSpace(info.Remarks))
                b.AddField("Remarks", info.Remarks);
            if (info.Aliases.Count > 1)
                b.AddField("Aliases", string.Join(", ", info.Aliases));
            if (info.Commands.Count > 0) {
                var commands = await GetAvailableCommands(info);
                b.AddField("Available Commands", FormatCommandList(commands));
            }
            if (info.Submodules.Count > 1)
                b.AddField("Submodules", FormatModuleList(info.Submodules));
            await Context.User.SendMessageAsync(string.Empty, embed: b.Build());
        }


        private struct CommandEntry : System.IComparable<CommandEntry> {
            public string Alias { get; }
            public CommandInfo Command { get; }
            public CommandEntry(string alias, CommandInfo command) {
                Alias = alias;
                Command = command;
            }

            int IComparable<CommandEntry>.CompareTo(CommandEntry other)
            {
                return Alias.CompareTo(other.Alias);
            }
        }

        private string FormatCommandList(IEnumerable<CommandInfo> commands) {
            var actualCommands = new List<CommandEntry>();
            foreach (var cmd in commands) {
                foreach (var alias in cmd.Aliases)
                    actualCommands.Add(new CommandEntry(alias, cmd));
            }
            actualCommands.Sort();

            var table = new TableWriter(3);
            foreach (var entry in actualCommands) {
                string cmdName = '`' + entry.Alias + '`';
                if (string.IsNullOrWhiteSpace(entry.Command.Summary)) {
                    table.AddRow(cmdName, "", "");
                } else {
                    table.AddRow(
                        cmdName,
                        "   ",
                        entry.Command.Summary
                    );
                }
            }
            return table.Write();
        }

        private string FormatModuleList(IEnumerable<ModuleInfo> modules) {
            var table = new TableWriter(3);
            foreach (var mod in modules) {
                string modName = '`' + mod.Aliases[0] + '`';
                if (string.IsNullOrWhiteSpace(mod.Summary))
                    table.AddRow(modName, "", "");
                else {
                    table.AddRow(modName, "   ", mod.Summary);
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine("```");
            table.Write(sb);
            sb.Append("```");
            return sb.ToString();
        }

        private ModuleInfo NavigatePath(string[] path) {
            if (path.Length == 0) {
                return null;
            }
            
            // navigate through the path and pick out what it designates
            var module = CommandService.Modules.FirstOrDefault(m => m.Aliases.Contains(path[0]));
            int currentIdx = 1;
            while (currentIdx < path.Length && module != null) {
                module = module.Submodules.FirstOrDefault(m => m.Aliases.Contains(path[currentIdx]));
                currentIdx++;
            }
            // we failed to find a module along the way!
            return module;
        }

        private Embed MakeCommandEmbed(CommandInfo cmd) {
            var b = new EmbedBuilder();
            b.WithTitle(cmd.Aliases[0])
             .WithColor(Constants.DiscordBlue);
            if (!string.IsNullOrWhiteSpace(cmd.Summary))
                b.WithDescription(cmd.Summary);
            if (!string.IsNullOrWhiteSpace(cmd.Remarks))
                b.AddField("Remarks", cmd.Remarks);
            if (cmd.Aliases.Count > 1)
                b.AddField("Aliases", string.Join(", ", cmd.Aliases));
            b.AddField("Usage", $"```{cmd.Aliases[0]} {GetCommandParametersInline(cmd)}```");
            var examples = GetExamples(cmd);
            if (examples.Count > 0)
                b.AddField("Examples", GetExampleLines(examples));
            if (cmd.Parameters.Count > 0)
                b.AddField("Parameters", GetCommandParameters(cmd));
            
            return b.Build();
        }

        private static List<UsageExampleAttribute> GetExamples(CommandInfo command) {
            var examples = new List<UsageExampleAttribute>();
            foreach (var attribute in command.Attributes) {
                if (attribute is UsageExampleAttribute) {
                    examples.Add(attribute as UsageExampleAttribute);
                }
            }
            return examples;
        }

        private static string GetCommandParameters(CommandInfo command) {
            var sb = new StringBuilder();
            bool isFirst = false;
            foreach (var p in command.Parameters) {
                if (!isFirst)
                    sb.AppendLine();
                isFirst = false;
                sb.Append('`');
                sb.Append(p.Name);
                sb.Append("` - ");

                sb.Append(p.Type.Name);
                if (p.IsOptional) {
                    sb.Append(", ");
                    sb.Append("optional");
                }
                if (p.DefaultValue != null) {
                    sb.Append(", ");
                    sb.Append("defaults to ");
                    sb.Append(p.DefaultValue.ToString());
                }
                if (p.IsMultiple) {
                    sb.Append(", ");
                    sb.Append("multiple arguments");
                }
                if (p.IsRemainder) {
                    sb.Append(", ");
                    sb.Append("collects the rest of the message");
                }
                sb.Append(". ");
                if (!string.IsNullOrWhiteSpace(p.Summary))
                    sb.AppendLine(p.Summary);
            }

            return sb.ToString();
        }

        private static string GetCommandParametersInline(CommandInfo command) {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var param in command.Parameters) {
                if (!isFirst)
                    sb.Append(' ');
                isFirst = false;
                if (param.IsRemainder) {
                    sb.Append(param.Name);
                    sb.Append("...");
                } else {
                    var brackets = param.IsOptional || param.DefaultValue != null;
                    if (brackets) sb.Append('[');
                    sb.Append(param.Name);
                    if (brackets) sb.Append(']');
                    else if (param.IsMultiple) sb.Append('*');
                }
            }
            return sb.ToString();
        }

        private static string GetExampleLines(List<UsageExampleAttribute> examples) {
            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (var example in examples) {
                if (!isFirst)
                    sb.AppendLine();
                isFirst = false;
                sb.Append("```");
                sb.Append(example.Usage);
                sb.AppendLine("```");
                sb.AppendLine(example.Description);
            }
            return sb.ToString();
        }

        private async Task<List<CommandInfo>> GetAvailableCommands() {
            var commands = new List<CommandInfo>();
            foreach (var module in CommandService.Modules)
                await CollectCommands(module, commands);
            return commands;
        }

        private async Task<List<CommandInfo>> GetAvailableCommands(ModuleInfo module) {
            var commands = new List<CommandInfo>();
            await CollectCommands(module, commands, false);
            return commands;
        }

        private async Task CollectCommands(ModuleInfo module, List<CommandInfo> commands, bool includeChildren=true) {
            foreach (var cmd in module.Commands) {
                var result = await cmd.CheckPreconditionsAsync(Context);
                if (result.IsSuccess)
                    commands.Add(cmd);
            }
            if (includeChildren) {
                foreach (var submodule in module.Submodules)
                    await CollectCommands(submodule, commands);
            }
        }
    }
}