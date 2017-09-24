using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using ReadMoreData;

namespace ReadMoreAPI
{
    public class DbMigrationsStartupFilter : IStartupFilter
    {
        private readonly ILogger<DbMigrationsStartupFilter> _logger;
        private readonly IHostingEnvironment _env;
        private readonly Func<IDbConnection> _dbConnFactory;

        public DbMigrationsStartupFilter(ILogger<DbMigrationsStartupFilter> logger, IHostingEnvironment env, Func<IDbConnection> dbConnFactory)
        {
            _logger = logger;
            _env = env;
            _dbConnFactory = dbConnFactory;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            _logger.LogInformation("Processing migrations...");
            var migrationFiles = _env.ContentRootFileProvider.GetDirectoryContents("/Migrations");
            using (var dbConn = _dbConnFactory())
            {
                MigrationsHandler.Migrate(dbConn, migrationFiles);
            }
            _logger.LogInformation("Migrations processed.");

            return next;
        }
    }
}
