using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace BookStoreApi.Services
{
    public class ReferenceConsoleRedisApp
    {
        static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" }
            });
        static async Task Main(string[] args)
        {
            var db = redis.GetDatabase();
            var pong = await db.PingAsync();
            Console.WriteLine(pong);
        }
    }
}