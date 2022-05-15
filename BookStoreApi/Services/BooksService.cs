using BookStoreApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookStoreApi.Services;

public class BooksService
{
    private readonly IMongoCollection<Book> _booksCollection;
    private readonly IDatabase redisDatabase;

    public BooksService(
        IOptions<BookStoreDatabaseSettings> bookStoreDatabaseSettings)
    {
        var mongoClient = new MongoClient(
            bookStoreDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            bookStoreDatabaseSettings.Value.DatabaseName);

         _booksCollection = mongoDatabase.GetCollection<Book>(
            bookStoreDatabaseSettings.Value.BooksCollectionName);

        var redis = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" }
            });

        redisDatabase = redis.GetDatabase();
    }

    public async Task<List<Book>> GetAsync() =>
        await _booksCollection.Find(_ => true).ToListAsync();

    public async Task<Book?> GetAsync(string id)
    {
        Book result;
        string redisBook = await redisDatabase.StringGetAsync(id);
        if (redisBook != null)
        {
            result = JsonSerializer.Deserialize<Book>(redisBook);
        }
        else
        {
            result = await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            // set in redis
            redisDatabase.StringSet(id, JsonSerializer.Serialize(result));
        }
        return result;
    }

    public async Task CreateAsync(Book newBook) =>
        await _booksCollection.InsertOneAsync(newBook);

    public async Task UpdateAsync(string id, Book updatedBook)
    {
        await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);
        redisDatabase.StringGetDelete(id);
    }
    public async Task RemoveAsync(string id)
    {
        await _booksCollection.DeleteOneAsync(x => x.Id == id);
        redisDatabase.StringGetDelete(id);
    }
}