using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordTestBot
{
    public class BasicTypeReader : TypeReader {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
        {
            if (Enum.TryParse<BasicType>(input, out BasicType type))
                return Task.FromResult(TypeReaderResult.FromSuccess(type));
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"`{input}` is not a valid basic type!"));
        }
    }
}