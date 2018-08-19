using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;
using static TextSplitter.TextBuilder;
using TextSplitter;

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
            string prefix = Configuration["prefix"];
            var usage = Separated(" ",
                Atomic($"Send commands to the bot by prefixing them with `{prefix}`."),
                Atomic($"For example, `{prefix}help echo` will show help for the `echo` command."),
                Atomic($"Use `{prefix}help help` for additional information about the `help` command."),
                Atomic($"For commands that have multiple words in them like `foo bar`, you can also query for any prefix, e.g. `{prefix}help foo` will show help about the `foo` module.")
            );

            var text = Separated(
                Glue(
                    Atomic("**Help**\n"),
                    usage
                ),
                Atomic("\n"),
                Atomic("\n"),
                Glue(
                    Atomic("**Available Commands**\n"),
                    Lines(FormatCommandList(await GetAvailableCommands()))
                )
            );
            await SendAsync(Context.User, text);
        }

        private async Task ShowCommandHelp(IEnumerable<CommandInfo> commands) {
            foreach (var cmd in commands) {
                var checkResult = await cmd.CheckPreconditionsAsync(Context);
                if (checkResult.IsSuccess) {
                    await SendAsync(Context.User, MakeCommandProvider(cmd));
                }
            }
        }

        private async Task ShowModuleHelp(ModuleInfo info) {
            IEnumerable<ITextProvider> Parts(List<CommandInfo> commands) {
                var header = Atomic($"**Module {info.Aliases[0]}**\n");
                if (!string.IsNullOrWhiteSpace(info.Summary))
                    yield return Glue(header, Atomic(info.Summary + '\n'));
                else
                    yield return header;

                if (!string.IsNullOrWhiteSpace(info.Remarks))
                    yield return Subsection("Remarks", Atomic(info.Remarks + '\n'));
                if (info.Aliases.Count > 1)
                    yield return Subsection("Aliases", Atomic(string.Join(", ", info.Aliases) + '\n'));
                if (info.Commands.Count > 0) {
                    yield return Subsection("Available Commands", Lines(FormatCommandList(commands)));
                }
                if (info.Submodules.Count > 1)
                    yield return Subsection("Submodules", Lines(FormatModuleList(info.Submodules)));
            }
            var commandList = await GetAvailableCommands(info);
            await SendAsync(Context.User, Separated(Parts(commandList)));
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

        private IEnumerable<ITextProvider> FormatCommandList(IEnumerable<CommandInfo> commands) {
            var actualCommands = new List<CommandEntry>();
            foreach (var cmd in commands) {
                foreach (var alias in cmd.Aliases)
                    actualCommands.Add(new CommandEntry(alias, cmd));
            }
            actualCommands.Sort();


            return actualCommands.Select(
                c => {
                    string cmdName = '`' + c.Alias + '`';
                    if (string.IsNullOrWhiteSpace(c.Command.Summary))
                        return Atomic(cmdName);
                    return Atomic(cmdName + " - " + c.Command.Summary);
                }
            );
        }

        private IEnumerable<ITextProvider> FormatModuleList(IEnumerable<ModuleInfo> modules) {
            return modules.Select (
                mod => {
                    string modName = '`' + mod.Aliases[0] + '`';
                    if (string.IsNullOrWhiteSpace(mod.Summary))
                        return Atomic(modName);
                    return Atomic(modName + " - " + mod.Summary);
                }
            );
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

        private ITextProvider MakeCommandProvider(CommandInfo cmd) {
            var chunks = new List<ITextProvider>();
            var header = Atomic($"**{cmd.Aliases[0]}**\n");
            if (!string.IsNullOrWhiteSpace(cmd.Summary))
                chunks.Add(Glue(header, Atomic(cmd.Summary + '\n')));
            else
                chunks.Add(header);
            
            if (!string.IsNullOrWhiteSpace(cmd.Remarks))
                chunks.Add(Subsection("Remarks", Atomic(cmd.Remarks + '\n')));
            
            if (cmd.Aliases.Count > 1) {
                chunks.Add(Subsection("Alias", Atomic(string.Join(", ", cmd.Aliases) + '\n')));
            }
            
            chunks.Add(Subsection("Usage", Atomic($"```{cmd.Aliases[0]} {GetCommandParametersInline(cmd)}```")));

            var examples = GetExamples(cmd);
            if (examples.Count > 0)
                chunks.Add(Subsection("Examples", Lines(GetExampleLines(examples))));
            if (cmd.Parameters.Count > 0)
                chunks.Add(Subsection("Parameters", Lines(GetCommandParameters(cmd))));
            return Separated(chunks);
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

        private static IEnumerable<ITextProvider> GetCommandParameters(CommandInfo command) {
            IEnumerable<ITextProvider> Parts(ParameterInfo p) {
                yield return Wrap("`", Atomic(p.Name));
                yield return Atomic(" - ");
                yield return Atomic(p.Type.Name);
                if (p.IsOptional)
                    yield return Atomic(", optional");
                if (p.DefaultValue != null)
                    yield return Atomic(", defaults to " + p.DefaultValue);
                if (p.IsMultiple)
                    yield return Atomic(", multiple arguments");
                if (p.IsRemainder)
                    yield return Atomic(", collects the rest of the message");
                yield return Atomic(".");
                if (!string.IsNullOrWhiteSpace(p.Summary))
                    yield return Atomic(p.Summary);
            }
            return command.Parameters.Select(p => Atomic(Parts(p)));
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

        private static IEnumerable<ITextProvider> GetExampleLines(List<UsageExampleAttribute> examples) {
            return examples.Select(e => 
                Atomic(
                    Wrap("```", Atomic(e.Usage)),
                    Atomic(e.Description)
                )
            );
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

        private static ITextProvider Subsection(string header, ITextProvider text) {
            return Glue(
                Atomic($"**{header}**\n"),
                text
            );
        }

        private static async Task SendAsync(IUser user, ITextProvider text) {
            const int DiscordMessageLimit = 2000;
            foreach (var msg in text.GetSections(DiscordMessageLimit))  {
                await user.SendMessageAsync(msg);
            }
        }
    }
}