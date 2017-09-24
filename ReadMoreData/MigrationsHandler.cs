using System;
using System.Data;
using System.Linq;
using Dapper;
using System.IO;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace ReadMoreData
{
    public static class MigrationsHandler
    {
        public const string CreateMigrationScriptsTable = @"CREATE TABLE IF NOT EXISTS ""MigrationScripts""(
            Name            TEXT        PRIMARY KEY   NOT NULL,
            ExecutionDate   TIMESTAMP                 NOT NULL
        )";

        // Based on example here: https://www.kenneth-truyers.net/2016/06/02/database-migrations-made-simple/
        public static IEnumerable<IFileInfo> Migrate(IDbConnection con, IEnumerable<IFileInfo> dirContents)
        {
            // Make sure that MigrationScripts table exists
            con.Execute(CreateMigrationScriptsTable);
            
            // Get all scripts that have been executed from the database
            var executedScripts = con.Query<string>("SELECT Name FROM \"MigrationScripts\"");

            // Get all scripts from the filesystem
            var scriptsToExecute = dirContents
                // filter the ones that have already been executed
                .Where(file => !executedScripts.Contains(file.Name))
                // Order by the date in the filename
                .OrderBy(file => DateTime.ParseExact(file.Name.Substring(0, 13), "yyyyMMdd-HHmm", null))
                .ToList();

            // got the list of scripts to execute, so lets execute them and
            // record that it was executed in the migrationscripts table
            foreach (var script in scriptsToExecute)
            {
                string sql;
                using (var readStream = script.CreateReadStream())
                using (var streamReader = new StreamReader(readStream))
                {
                    sql = streamReader.ReadToEnd();
                }
                
                con.Execute(sql);
                con.Execute(
                    "INSERT INTO \"MigrationScripts\" (Name, ExecutionDate) VALUES (@Name, current_timestamp)",
                    new { script.Name });
            }

            return scriptsToExecute;
        }
    }
}
