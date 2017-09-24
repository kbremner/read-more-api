﻿using System;
using System.Data;
using System.Threading.Tasks;
using ReadMoreData.Models;
using Dapper;

namespace ReadMoreData
{
    public class DbPocketAccountsRepository : DbConnectionRepository, IPocketAccountsRepository
    {
        private const string TableName = "\"PocketAccounts\"";

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

        public async Task<PocketAccount> InsertAsync(PocketAccount account)
        {
            using (var conn = Connection)
            {
                return await conn.QuerySingleAsync<PocketAccount>(
                    $"INSERT INTO {TableName} (AccessToken, RequestToken, RedirectUrl) " +
                    "VALUES (@AccessToken, @RequestToken, @RedirectUrl) returning *", account);
            }
        }

        public async Task UpdateAsync(PocketAccount account)
        {
            using (var conn = Connection)
            {
                await conn.ExecuteAsync($"UPDATE {TableName} " +
                    "SET AccessToken = @AccessToken, RequestToken = @RequestToken, RedirectUrl = @RedirectUrl " +
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
    }
}
