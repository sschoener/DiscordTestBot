using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DiscordTestBot
{
    public class MongoDBService {
        private readonly IConfigurationRoot _configuration;
        public MongoClient Client { get; private set; }
        public IMongoDatabase Database { get; private set; }
        public MongoDBService(IConfigurationRoot configuration) {
            _configuration = configuration;
        }

        public void Start() {
            string address = _configuration["mongodb:address"];
            Client = new MongoClient(address);
            Database = Client.GetDatabase(_configuration["mongodb:database"]);
        }
    }
}