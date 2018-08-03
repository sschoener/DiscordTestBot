using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using System.Text;

namespace DiscordTestBot
{
    [Group("db")]
    [Summary("Contains commands that help with database debugging.")]
    public class MongoDBModule : ModuleBase {
        public MongoDBService MongoDB { get; set; }

        [Command("list")] // this causes the command to use the name of the group it is in
        [Summary("Prints out information about a database.")]
        [UsageExample("db list", "Prints out all databases")]
        [UsageExample("db list test", "Prints out all collections in the database `test`.")]
        [UsageExample("db list test coll", "Prints out information about collection `coll` in database `test`.")]
        public async Task ListDatabases([Summary("Database to query.")] string dbName=null,
                                        [Summary("Collection to query.")] string collectionName=null,
                                        [Summary("How many items to show at most from the collection.")] int limit=10) {
            var query = await MongoDB.Client.ListDatabaseNamesAsync();
            var dbs = query.ToList();
            if (dbName == null) {
                await QueryResult("Databases", dbs);
                return;
            } else if (!dbs.Contains(dbName)) {
                await Error($"Unknown database {dbName}");
                return;
            }
            
            var db = MongoDB.Client.GetDatabase(dbName);
            var collections = (await db.ListCollectionNamesAsync()).ToEnumerable();
            if (collectionName == null) {
                await QueryResult("Collections", collections);
                return;
            } else if (!collections.Contains(collectionName)) {
                await Error($"Unknown collection {collectionName} in database {dbName}");
                return;
            } else {
                var collection = db.GetCollection<BsonDocument>(collectionName);
                var filter = Builders<BsonDocument>.Filter.Empty;
                var count = await collection.CountDocumentsAsync(filter);
                var collectionQuery = collection.Find(filter).Limit(limit).ToEnumerable();
                await QueryResult($"Entries in {collectionName} ({count})", collectionQuery);
                return;
            }
        }

        [Command("find")]
        [Summary("Finds an entry in a database.")]
        [UsageExample("db find test coll int ID 3", "Searches the collection `coll` in database `test` for entries with ID = 3.")]
        [UsageExample("db find test coll string Name Paul 10", "Searches the collection `coll` in database `test` for at most 10 entries with Name = Paul.")]
        public async Task FindInDatabase([Summary("Database to query.")] string dbName,
                                         [Summary("Collection to query.")] string collectionName,
                                         [Summary("Field to query.")] string field,
                                         [Summary("Type of field to query.")] BasicType type,
                                         [Summary("Value to match against.")] string value,
                                         [Summary("How many values to include at most.")] int limit=1,
                                         [Summary("How many values to skip.")] int skip=0) {
            var collection = await GetCollection<BsonDocument>(dbName, collectionName);
            if (collection == null)
                return;
            if (!type.TryParseValue(value, out object parsedValue)) {
                await Error($"Failed to parse {value} as {type}.");
                return;
            }
            var query = await collection.Find(
                Builders<BsonDocument>.Filter.Eq(field, parsedValue)
            ).Limit(limit).Skip(skip).ToCursorAsync();
            await QueryResult<BsonDocument>("Query result", query);
        }

        private async Task<IMongoCollection<T>> GetCollection<T>(string dbName, string collectionName) {
            var query = await MongoDB.Client.ListDatabaseNamesAsync();
            var dbs = query.ToList();
            if (!dbs.Contains(dbName)) {
                await Error($"Unknown database {dbName}");
                return null;
            }
            
            var db = MongoDB.Client.GetDatabase(dbName);
            var collections = (await db.ListCollectionNamesAsync()).ToList();
            if (!collections.Contains(collectionName)) {
                await Error($"Unknown collection {collectionName} in database {dbName}");
                return null;
            } 
            return db.GetCollection<T>(collectionName);
        }

        private async Task Error(string message) {
            var embed = new EmbedBuilder()
                .WithColor(Constants.ErrorRed)
                .WithTitle("Error")
                .WithDescription(message)
                .Build();
            await Context.User.SendMessageAsync("", embed: embed);
        }

        private async Task<string> Join<T>(string separator, IAsyncCursor<T> data) {
            var sb = new StringBuilder();
            bool first = true;
            while(await data.MoveNextAsync()) {
                foreach (var d in data.Current) {
                    if (!first)
                        sb.Append(separator);
                    else
                        first = false;
                    sb.Append(d);
                }
            }
            return sb.ToString();
        }

        private async Task QueryResult<T>(string title, IAsyncCursor<T> results) {
            var embed = new EmbedBuilder()
                .WithColor(Constants.DiscordBlue)
                .WithTitle(title)
                .WithDescription(await Join("\n", results))
                .Build();
            await Context.User.SendMessageAsync("", embed: embed);
        }

        private async Task QueryResult<T>(string title, IEnumerable<T> results) {
            var embed = new EmbedBuilder()
                .WithColor(Constants.DiscordBlue)
                .WithTitle(title)
                .WithDescription(string.Join('\n', results))
                .Build();
            await Context.User.SendMessageAsync("", embed: embed);
        }
    }
}