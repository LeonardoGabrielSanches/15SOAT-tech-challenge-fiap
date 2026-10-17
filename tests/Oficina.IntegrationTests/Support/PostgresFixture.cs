using Npgsql;
using Testcontainers.PostgreSql;

namespace Oficina.IntegrationTests.Support;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database }.ConnectionString;

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Nome)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "postgres";
}
