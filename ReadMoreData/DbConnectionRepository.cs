using System;
using System.Data;

namespace ReadMoreData
{
    public abstract class DbConnectionRepository
    {
        private readonly Func<IDbConnection> _connectionFactory;

        protected DbConnectionRepository(Func<IDbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected IDbConnection Connection
        {
            get
            {
                var conn = _connectionFactory();
                conn.Open();
                return conn;
            }
        }
    }
}
