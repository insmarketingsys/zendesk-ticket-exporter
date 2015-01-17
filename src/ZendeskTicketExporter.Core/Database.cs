﻿using Dapper;
using LiteGuard;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ZendeskTicketExporter.Core
{
    public class Database : IDatabase
    {
        private readonly string _dbFile;

        public Database(string dbFile)
        {
            _dbFile = dbFile;
        }

        public async Task ExecuteAsync(string sql, object param = null)
        {
            Guard.AgainstNullArgument("sql", sql);

            using (var conn = await GetOpenConnectionAsync())
            {
                await Task.Factory.StartNew(() => conn.Execute(sql, param));
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            Guard.AgainstNullArgument("sql", sql);

            using (var conn = await GetOpenConnectionAsync())
            {
                return await conn.QueryAsync<T>(sql, param);
            }
        }

        public async Task<T> QueryScalerAsync<T>(string sql, object param = null)
        {
            Guard.AgainstNullArgument("sql", sql);

            using (var conn = await GetOpenConnectionAsync())
            {
                var results = await conn.QueryAsync<T>(sql, param);
                return results.First();
            }
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            Guard.AgainstNullArgument("tableName", tableName);

            using (var conn = await GetOpenConnectionAsync())
            {
                // Adapted from http://stackoverflow.com/a/1604121/941536
                var results = await conn.QueryAsync<long>(string.Format(
                    "select count(*) from sqlite_master where type='table' and name='{0}'",
                    tableName));

                return results.First() > 0;
            }
        }

        protected virtual async Task<IDbConnection> GetOpenConnectionAsync()
        {
            CreateDatabaseIfNecessary();

            var conn = new SQLiteConnection(string.Format(@"Data Source={0};Version=3;", _dbFile));
            await conn.OpenAsync();

            return conn;
        }

        public async Task DropTable(string tableName)
        {
            await ExecuteAsync(string.Format("drop table if exists {0} ;", tableName));
        }

        private void CreateDatabaseIfNecessary()
        {
            if (File.Exists(_dbFile) == false)
                SQLiteConnection.CreateFile(_dbFile);
        }
    }
}