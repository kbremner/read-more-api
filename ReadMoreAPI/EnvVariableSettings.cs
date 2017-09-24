// ReSharper disable InconsistentNaming, UnassignedGetOnlyAutoProperty, MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
using System;

namespace ReadMoreAPI
{
    public class EnvVariableSettings
    {
        public string POCKET_CONSUMER_KEY { get; set; }
        public string DATABASE_URL { get; set; }

        /// <summary>
        /// DATABASE_URL in the format required by Npgsql for connection strings.
        /// This ensures that we can cope with the DATABASE_URL format used
        /// by Heroku when the application is deployed.
        /// </summary>
        public string NpgsqlConnectionString
        {
            get
            {
                var dbUri = new Uri(DATABASE_URL);
                var splitUserInfo = dbUri.UserInfo.Split(':');
                var dbConnString = $"Host={dbUri.Host};Username={splitUserInfo[0]};Password={splitUserInfo[1]};Database={dbUri.PathAndQuery.Substring(1)}";
                if (dbUri.Port != -1)
                {
                    dbConnString += $";Port={dbUri.Port}";
                }
                return dbConnString;
            }
        }
    }
}