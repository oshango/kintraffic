using KinTraffic.Api.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Tests.TestHelpers;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<KinTrafficDbContext> _options;

    private SqliteTestDatabase(SqliteConnection connection, DbContextOptions<KinTrafficDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<KinTrafficDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new KinTrafficDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new SqliteTestDatabase(connection, options);
    }

    public KinTrafficDbContext CreateContext() => new(_options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}