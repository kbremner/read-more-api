using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Extensions.FileProviders;
using ReadMoreData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;

namespace ReadMoreDataTests
{
    [ExclusivelyUses("Database")]
    [TestCategory("DB Tests")]
    [TestClass]
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class MigrationsHandlerTests
    {
        private PostgresTestHelper _postgresTestHelper;

        [TestInitialize]
        public void Setup()
        {
            _postgresTestHelper = new PostgresTestHelper();
            _postgresTestHelper.ClearDb();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _postgresTestHelper.Dispose();
        }

        [TestMethod]
        public void CreatesMigrationScriptsTable()
        {
            Assert.IsFalse(_postgresTestHelper.DoesTableExist("MigrationScripts"));

            MigrationsHandler.Migrate(_postgresTestHelper.Connection, new IFileInfo[0]);
            
            Assert.IsTrue(_postgresTestHelper.DoesTableExist("MigrationScripts"));
        }

        [TestMethod]
        public void MigrationScriptsTableStillExistsIfAlreadyPresent()
        {
            _postgresTestHelper.Connection.Execute(MigrationsHandler.CreateMigrationScriptsTable);
            Assert.IsTrue(_postgresTestHelper.DoesTableExist("MigrationScripts"));

            MigrationsHandler.Migrate(_postgresTestHelper.Connection, new IFileInfo[0]);

            Assert.IsTrue(_postgresTestHelper.DoesTableExist("MigrationScripts"));
        }

        [TestMethod]
        public void RecordsExecutedScriptNamesInMigrationScriptsTable()
        {
            var files = PostgresTestHelper.GetMigrationScripts();

            var result = MigrationsHandler.Migrate(_postgresTestHelper.Connection, files).ToList();

            var executedScripts = _postgresTestHelper.GetExecutedScripts().ToList();

            foreach (var file in files)
            {
                CollectionAssert.Contains(executedScripts, file.Name, $"Executed scripts did not contain script {file.Name}");
                CollectionAssert.Contains(result, file, $"Migration result did not contain file {file.Name}");
            }
        }

        [TestMethod]
        public void ExecutesScriptsNotInMigrationScriptsTable()
        {
            var files = PostgresTestHelper.GetMigrationScripts();
            var excludedFileName = files[files.Length - 1].Name;
            _postgresTestHelper.Connection.Execute(MigrationsHandler.CreateMigrationScriptsTable);
            _postgresTestHelper.AddExecutedScript(excludedFileName);

            var expectedExecuted = files.Where(x => x.Name != excludedFileName);

            var result = MigrationsHandler.Migrate(_postgresTestHelper.Connection, files);

            var executedScripts = _postgresTestHelper.GetExecutedScripts();

            foreach (var file in files)
            {
                Assert.IsTrue(executedScripts.Contains(file.Name), $"Executed scripts did not contain script {file.Name}");
            }

            foreach (var file in expectedExecuted)
            {
                Assert.IsTrue(result.Contains(file), $"Expected script {file.Name} to be executed but it was not");
            }
        }

        [TestMethod]
        public void ExecutesScriptsInDateOrder()
        {
            var files = PostgresTestHelper.GetMigrationScripts();
            _postgresTestHelper.Connection.Execute(MigrationsHandler.CreateMigrationScriptsTable);

            var result = MigrationsHandler
                .Migrate(_postgresTestHelper.Connection, files)
                .Select(x => x.Name)
                .ToArray();

            var executedScripts = _postgresTestHelper.GetExecutedScripts().ToArray();
            var fileNames = files
                .Select(x => x.Name)
                .OrderBy(file => DateTime.ParseExact(file.Substring(0, 13), "yyyyMMdd-HHmm", null))
                .ToArray();
            Assert.AreEqual(fileNames.Length, executedScripts.Length);
            Assert.AreEqual(fileNames.Length, result.Length);
            for (var i = 0; i < files.Length; i++)
            {
                Assert.AreEqual(fileNames[i], executedScripts[i]);
                Assert.AreEqual(fileNames[i], result[i]);
            }
        }

        [TestMethod]
        public void ExecutesContentsOfScripts()
        {
            var files = PostgresTestHelper.GetMigrationScripts();
            _postgresTestHelper.Connection.Execute(MigrationsHandler.CreateMigrationScriptsTable);

            MigrationsHandler.Migrate(_postgresTestHelper.Connection, files);

            Assert.IsTrue(_postgresTestHelper.DoesTableExist("XmlKeys"));
            Assert.IsTrue(_postgresTestHelper.DoesTableExist("PocketAccounts"));
        }
    }
}