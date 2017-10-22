using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ReadMoreData.Models;
using Dapper;

namespace ReadMoreData
{
    public class DbPocketAccountsRepository : DbConnectionRepository, IPocketAccountsRepository
    {
        private const string TableName = "\"PocketAccounts\"";
        private const string FeatureToggleTableName = "\"FeatureToggles\"";
        private const string FeatureToggleJoinTableName = "\"PocketAccountFeatureToggles\"";

        public DbPocketAccountsRepository(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<PocketAccount> FindByIdAsync(Guid id)
        {
            using (var conn = Connection)
            {
                return await conn.QuerySingleAsync<PocketAccount>(
                    $"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = id });
            }
        }

        public async Task<PocketAccount> FindByUsernameAsync(string username)
        {
            using (var conn = Connection)
            {
                return await conn.QuerySingleOrDefaultAsync<PocketAccount>(
                    $"SELECT * FROM {TableName} WHERE Username = @Username", new { Username = username });
            }
        }

        public async Task<PocketAccount> InsertAsync(PocketAccount account)
        {
            using (var conn = Connection)
            {
                return await conn.QuerySingleAsync<PocketAccount>(
                    $"INSERT INTO {TableName} (AccessToken, RequestToken, RedirectUrl, Username) " +
                    "VALUES (@AccessToken, @RequestToken, @RedirectUrl, @Username) returning *", account);
            }
        }

        public async Task UpdateAsync(PocketAccount account)
        {
            using (var conn = Connection)
            {
                await conn.ExecuteAsync($"UPDATE {TableName} " +
                    "SET AccessToken = @AccessToken, RequestToken = @RequestToken, RedirectUrl = @RedirectUrl, Username = @Username " +
                    "WHERE Id = @Id", account);
            }
        }

        public async Task DeleteAsync(PocketAccount account)
        {
            using (var conn = Connection)
            {
                await conn.ExecuteAsync($"DELETE FROM {TableName} WHERE Id = @Id", account);
            }
        }

        public async Task<IEnumerable<FeatureToggle>> FindTogglesForAccountAsync(Guid id)
        {
            using (var conn = Connection)
            {
                return await conn.QueryAsync<FeatureToggle>(
                    $"SELECT t.* FROM {FeatureToggleTableName} t " +
                    $"INNER JOIN {FeatureToggleJoinTableName} a " +
                    "ON a.ToggleId = t.Id " +
                    "WHERE a.AccountId = @Id", new { Id = id });
            }
        }
    }
}
