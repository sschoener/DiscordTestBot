using System.Threading.Tasks;
using MongoDB.Driver;

namespace DiscordTestBot
{
    public class UserScoreService {
        private readonly MongoDBService _mongoDB;
        private IMongoCollection<UserScore> _scores;
        public IMongoCollection<UserScore> Scores {
            get {
                if (_scores == null)
                    _scores = _mongoDB.Database.GetCollection<UserScore>("UserScore");
                return _scores;
            }
        }

        public UserScoreService(MongoDBService mongoDB) {
            _mongoDB = mongoDB;
        }

        public async Task<int> GetScoreAsync(ulong userID) {
            var results = await Scores.FindAsync(us => us.UserID == userID);
            var userEntry = await results.FirstOrDefaultAsync();
            if (userEntry != null)
                return userEntry.Score;
            return 0;
        }

        public async Task SetScoreAsync(ulong userID, int score) {
            await Scores.ReplaceOneAsync(
                us => us.UserID == userID,
                new UserScore { UserID = userID, Score = score },
                // instruct the DB to insert this entry if it has not been found
                new UpdateOptions { IsUpsert = true }
            );
        }

        public class UserScore {
            [MongoDB.Bson.Serialization.Attributes.BsonId]
            public MongoDB.Bson.ObjectId _id; 
            public ulong UserID;
            public int Score;
        }
    }
}