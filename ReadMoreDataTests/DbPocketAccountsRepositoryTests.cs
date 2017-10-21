using System;
using System.Data.Common;
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
    public class DbPocketAccountsRepositoryTests
    {
        private PostgresTestHelper _postgresTestHelper;

        [TestInitialize]
        public void Setup()
        {
            _postgresTestHelper = new PostgresTestHelper();
            _postgresTestHelper.SetupDb();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _postgresTestHelper.Dispose();
        }

        [TestMethod]
        public async Task CanInsertAccount()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token",
                Username = "user-name"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);

            var result = await repo.InsertAsync(account);

            Assert.AreEqual(account.AccessToken, result.AccessToken);
            Assert.AreEqual(account.RedirectUrl, result.RedirectUrl);
            Assert.AreEqual(account.RequestToken, result.RequestToken);
            Assert.AreEqual(account.Username, result.Username);
        }

        [TestMethod]
        public async Task ReturnsInsertedAccount()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token",
                Username = "user-name"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);

            var result = await repo.InsertAsync(account);
            var actualAccount = await _postgresTestHelper.Connection.QuerySingleAsync<PocketAccount>(
                @"select * from ""PocketAccounts"" where Id = @Id", result);

            Assert.AreEqual(result.Id, actualAccount.Id);
            Assert.AreEqual(result.AccessToken, actualAccount.AccessToken);
            Assert.AreEqual(result.RedirectUrl, actualAccount.RedirectUrl);
            Assert.AreEqual(result.RequestToken, actualAccount.RequestToken);
            Assert.AreEqual(result.Username, actualAccount.Username);
            Assert.AreEqual(result.EmailUserId, actualAccount.EmailUserId);
        }

        [TestMethod]
        public async Task CanUpdateAccount()
        {
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token",
                Username = "user-name"
            });

            var updatedAccount = new PocketAccount
            {
                Id = insertedAccount.Id,
                AccessToken = "access-token2",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token2",
                Username = "user-name2",
                EmailUserId = insertedAccount.EmailUserId
            };
            await repo.UpdateAsync(updatedAccount);
            var actualAccount = await _postgresTestHelper.Connection.QuerySingleAsync<PocketAccount>(
                @"select * from ""PocketAccounts"" where Id = @Id", updatedAccount);


            Assert.AreEqual(updatedAccount.Id, actualAccount.Id);
            Assert.AreEqual(updatedAccount.AccessToken, actualAccount.AccessToken);
            Assert.AreEqual(updatedAccount.RedirectUrl, actualAccount.RedirectUrl);
            Assert.AreEqual(updatedAccount.RequestToken, actualAccount.RequestToken);
            Assert.AreEqual(updatedAccount.Username, actualAccount.Username);
            Assert.AreEqual(updatedAccount.EmailUserId, actualAccount.EmailUserId);
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
                RequestToken = "request-token",
                Username = "user-name"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(account);

            var result = await repo.FindByIdAsync(insertedAccount.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(insertedAccount.Id, result.Id);
            Assert.AreEqual(insertedAccount.RequestToken, result.RequestToken);
            Assert.AreEqual(insertedAccount.AccessToken, result.AccessToken);
            Assert.AreEqual(insertedAccount.RedirectUrl, result.RedirectUrl);
            Assert.AreEqual(insertedAccount.Username, result.Username);
            Assert.AreEqual(insertedAccount.EmailUserId, result.EmailUserId);
        }

        [TestMethod]
        public async Task CanFindByUsername()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token",
                Username = "user-name"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            var insertedAccount = await repo.InsertAsync(account);

            var result = await repo.FindByUsernameAsync(insertedAccount.Username);

            Assert.IsNotNull(result);
            Assert.AreEqual(insertedAccount.Id, result.Id);
            Assert.AreEqual(insertedAccount.RequestToken, result.RequestToken);
            Assert.AreEqual(insertedAccount.AccessToken, result.AccessToken);
            Assert.AreEqual(insertedAccount.RedirectUrl, result.RedirectUrl);
            Assert.AreEqual(insertedAccount.Username, result.Username);
            Assert.AreEqual(insertedAccount.EmailUserId, result.EmailUserId);
        }

        [TestMethod]
        public async Task InsertFailsIfUsernameIsNotUnique()
        {
            var account = new PocketAccount
            {
                AccessToken = "access-token",
                RedirectUrl = "http://example.com",
                RequestToken = "request-token",
                Username = "user-name"
            };
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);
            await repo.InsertAsync(account);

            try
            {
                await repo.InsertAsync(account);
                Assert.Fail("Expected second insert to fail");
            }
            catch (DbException)
            {
            }
        }

        [TestMethod]
        public async Task FindByUsernameReturnsNullIfNoneFound()
        {
            var repo = new DbPocketAccountsRepository(_postgresTestHelper.ConnectionFactory);

            var result = await repo.FindByUsernameAsync("user-name");

            Assert.IsNull(result);
        }
    }
}
