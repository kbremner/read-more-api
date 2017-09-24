using System;
using System.Collections.Generic;
using System.Data;
using ReadMoreData.Models;
using Dapper;

namespace ReadMoreData
{
    public class DbXmlKeysRepository : DbConnectionRepository, IXmlKeysRepository
    {
        private const string TableName = "\"XmlKeys\"";

        public DbXmlKeysRepository(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
        }

        public Guid Add(XmlKey key)
        {
            using (var conn = Connection)
            {
                return conn.ExecuteScalar<Guid>($"INSERT into {TableName}(Xml) values (@Xml) returning Id", key);
            }
        }

        public IEnumerable<XmlKey> FindAll()
        {
            using (var conn = Connection)
            {
                return conn.Query<XmlKey>($"select * from {TableName}");
            }
        }
    }
}
