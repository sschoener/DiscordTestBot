using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordTestBot {
    [Group("test")]
    public class EchoModule : ModuleBase {

        [Command("echo"), Summary("Echoes a message back to the user. Useful for testing.")]
        [UsageExample("test echo Hello World!", "Echoes back 'Hello World!'.")]
        public async Task Echo([Remainder, Summary("The text to echo.")] string msg)
        {
            // assume that we can delete message everywhere but in private DMs to our bot.
            if (!(Context.Channel is IDMChannel))
                await Context.Message.DeleteAsync();
            await Context.User.SendMessageAsync(msg);
        }
    }
}