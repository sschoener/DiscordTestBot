using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordTestBot
{
    /// <summary>
    /// Dummy-module that demonstrates how to persist data with MongoDB.
    /// It stores a per-user score that is incremented whenever the user sends the `score` command.
    /// </summary>
    public class UserScoreModule : ModuleBase {
        public UserScoreService UserScore { get; set; }

        [Command("score")]
        [Summary("Increments your score by 1 and prints it out.")]
        public async Task ScoreIncrement() {
            var score = await UserScore.GetScoreAsync(Context.User.Id);
            int newScore = score + 1;
            await UserScore.SetScoreAsync(Context.User.Id, newScore);
            await ReplyAsync($"Your score is now {newScore}.");
        }
    }
}