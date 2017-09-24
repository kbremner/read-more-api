using System;
using System.Linq;
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
    public class DbXmlKeysRepositoryTests : IDisposable
    {
        private readonly PostgresTestHelper _postgresTestHelper;

        public DbXmlKeysRepositoryTests()
        {
            _postgresTestHelper = new PostgresTestHelper();
            _postgresTestHelper.SetupDb();
        }

        [TestMethod]
        public void AddsElementToTable()
        {
            const string xml = "<tag></tag>";
            var repo = new DbXmlKeysRepository(_postgresTestHelper.ConnectionFactory);

            var insertedKey = repo.Add(new XmlKey {Xml = xml});

            var actualKey = _postgresTestHelper.Connection.QueryFirst<XmlKey>(
                @"Select * from ""XmlKeys"" where Id = @Id",
                new { Id = insertedKey });
            Assert.AreEqual(xml, actualKey.Xml);
        }

        [TestMethod]
        public void UsesNewConnectionForEachAdd()
        {
            const string xml = "<tag></tag>";
            var count = 0;
            var repo = new DbXmlKeysRepository(() =>
            {
                count++;
                return _postgresTestHelper.ConnectionFactory();
            });

            repo.Add(new XmlKey { Xml = xml });
            repo.Add(new XmlKey { Xml = xml });

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void CanFindAll()
        {
            var keys = new[]
            {
                new XmlKey {Xml = "<tag1></tag1>"},
                new XmlKey {Xml = "<tag2></tag2>"}
            };
            foreach (var key in keys)
            {
                key.Id = _postgresTestHelper.Connection.ExecuteScalar<Guid>(@"INSERT into ""XmlKeys""(Xml) values (@Xml) returning Id", key);
            }
            var repo = new DbXmlKeysRepository(_postgresTestHelper.ConnectionFactory);

            var actualKeys = repo.FindAll().ToArray();

            Assert.AreEqual(keys.Length, actualKeys.Length);

            foreach (var key in keys)
            {
                Assert.IsTrue(actualKeys.Any(x => x.Id == key.Id && x.Xml == key.Xml), $"DB did not contain key with ID {key.Id}");
            }
        }

        [TestMethod]
        public void UsesNewConnectionForEachFindCall()
        {
            var count = 0;
            var repo = new DbXmlKeysRepository(() =>
            {
                count++;
                return _postgresTestHelper.ConnectionFactory();
            });

            repo.FindAll();
            repo.FindAll();

            Assert.AreEqual(2, count);
        }


        public void Dispose()
        {
            _postgresTestHelper.Dispose();
        }
    }
}
