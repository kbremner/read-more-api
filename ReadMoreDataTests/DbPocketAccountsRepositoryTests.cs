using System;
using System.Threading.Tasks;
using Dapper;
using ReadMoreData;
using ReadMoreData.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;

namespace ReadMoreDataTests
{
    [ExclusivelyUses("Database")]
    [TestCategory("DB Tests")]
    [TestClass]
    public class DbPocketAccountsRepositoryTests : IDisposable
    {
        private readonly PostgresTestHelper _postgresTestHelper;

        public DbPocketAccountsRepositoryTests()
        {
            _postgresTestHelper = new PostgresTestHelper();
            _postgresTestHelper.SetupDb();
        }

        [TestMethod]
        public async Task CanInsertAccount()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);

            var result = await repo.InsertAsync(account);

            Assert.AreEqual(account.AccessToken, result.AccessToken);
            Assert.AreEqual(account.RedirectUrl, result.RedirectUrl);
            Assert.AreEqual(account.RequestToken, result.RequestToken);
        }

        [TestMethod]
        public async Task ReturnsInsertedAccount()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);

            var result = await repo.InsertAsync(account);
            var actualAccount = await _postgresTestHelper.Connection.QuerySingleAsync<PocketAccount>(
                @"select * from ""PocketAccounts"" where Id = @Id", result);

            Assert.AreEqual(result.Id, actualAccount.Id);
            Assert.AreEqual(result.AccessToken, actualAccount.AccessToken);
            Assert.AreEqual(result.RedirectUrl, actualAccount.RedirectUrl);
            Assert.AreEqual(result.RequestToken, actualAccount.RequestToken);
        }

        [TestMethod]
        public async Task CanUpdateAccount()
        {
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token"
            });

            var updatedAccount = new PocketAccount
            {
                Id = insertedAccount.Id,
                AccessToken = "access-token2",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token2"
            };
            await repo.UpdateAsync(updatedAccount);
            var actualAccount = await _postgresTestHelper.Connection.QuerySingleAsync<PocketAccount>(
                @"select * from ""PocketAccounts"" where Id = @Id", updatedAccount);


            Assert.AreEqual(updatedAccount.Id, actualAccount.Id);
            Assert.AreEqual(updatedAccount.AccessToken, actualAccount.AccessToken);
            Assert.AreEqual(updatedAccount.RedirectUrl, actualAccount.RedirectUrl);
            Assert.AreEqual(updatedAccount.RequestToken, actualAccount.RequestToken);
        }

        [TestMethod]
        public async Task CanDeleteAccount()
        {
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token"
            });

            await repo.DeleteAsync(insertedAccount);
            var actualAccount = await _postgresTestHelper.Connection.QuerySingleOrDefaultAsync<PocketAccount>(
                @"SELECT * FROM ""PocketAccounts"" WHERE Id = @Id", insertedAccount);

            Assert.IsNull(actualAccount);
        }

        [TestMethod]
        public async Task CanFindById()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(account);

            var result = await repo.FindByIdAsync(insertedAccount.Id);

            Assert.AreEqual(insertedAccount.Id, result.Id);
            Assert.AreEqual(insertedAccount.RequestToken, result.RequestToken);
            Assert.AreEqual(insertedAccount.AccessToken, result.AccessToken);
            Assert.AreEqual(insertedAccount.RedirectUrl, result.RedirectUrl);
        }

        public void Dispose()
        {
            _postgresTestHelper.Dispose();
        }
    }
}
