using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Extensions.FileProviders;
using ReadMoreData;

namespace ReadMoreDataTests
{
    public class PostgresTestHelper : IDisposable
    {
        private const string DefaultConnectionString =
            "Host=localhost;Port=32770;Username=readmore-test;Password=readmore-test;Database=readmore-test;";

        private readonly string _connectionString;

        public PostgresTestHelper(string connectionString = null)
        {
            _connectionString = connectionString
                ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? DefaultConnectionString;
            Connection = ConnectionFactory();
        }

        public IDbConnection Connection { get; }

        public Func<IDbConnection> ConnectionFactory
        {
            get { return () => new Npgsql.NpgsqlConnection(_connectionString); }
        }

        public void ClearDb()
        {
            Connection.Execute(@"DROP owned BY ""readmore-test""");
        }

        public bool DoesTableExist(string tableName)
        {
            var result = Connection.ExecuteScalar<int>(@"SELECT count(*)
            FROM information_schema.tables
            WHERE table_name = @tableName", new { tableName });
            return result == 1;
        }

        public IEnumerable<string> GetExecutedScripts()
        {
            return Connection.Query<string>(@"SELECT Name FROM ""MigrationScripts""");
        }

        public static IFileInfo[] GetMigrationScripts()
        {
            var folder = Directory.GetCurrentDirectory() + @"/../../../../ReadMoreAPI/Migrations/";
            folder = Path.GetFullPath(folder);
            var fileProvider = new PhysicalFileProvider(folder);
            return fileProvider.GetDirectoryContents("").ToArray();
        }

        public void AddExecutedScript(string scriptName)
        {
            Connection.Execute(
                @"INSERT INTO ""MigrationScripts"" (Name, ExecutionDate) VALUES (@Name, current_timestamp)",
                new { Name = scriptName });
        }

        public void SetupDb()
        {
            ClearDb();
            Connection.Execute(MigrationsHandler.CreateMigrationScriptsTable);
            MigrationsHandler.Migrate(Connection, GetMigrationScripts());
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}
