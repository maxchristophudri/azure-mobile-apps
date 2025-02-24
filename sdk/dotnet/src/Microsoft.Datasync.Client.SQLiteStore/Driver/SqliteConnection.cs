﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Datasync.Client.Utils;
using SQLitePCL;

namespace Microsoft.Datasync.Client.SQLiteStore.Driver
{
    internal class SqliteConnection
    {
        /// <summary>
        /// The maximum number of parameters per query.  See https://www.sqlite.org/limits.html#max_variable_number
        /// </summary>
        public int MaxParametersPerQuery { get; }

        /// <summary>
        /// Set to <c>true</c> once the SQLitePCL library is configured.
        /// </summary>
        internal static bool sqliteIsInitialized;

        /// <summary>
        /// The SQLite database connection.
        /// </summary>
        internal sqlite3 connection;

        /// <summary>
        /// Creates a new <see cref="SqliteConnection"/> to execute SQLite commands.
        /// </summary>
        /// <remarks>
        /// <para>If the connection string starts with <c>file:</c>, then it is considered to be a URI filename and
        /// should be structured as such.  This allows the setting of any options (such as mode, cache, etc.)
        /// if needed.</para>
        /// <para>If the connection string does not start with file:, then it should be an absolute path (which starts
        /// with a <c>/</c>).</para>
        /// </remarks>
        /// <param name="connectionString">The connection string to use.</param>
        public SqliteConnection(string connectionString)
        {
            if (!sqliteIsInitialized)
            {
                Batteries_V2.Init();

                // You only need to configure the sqlite3 interface once.
                if (raw.sqlite3_config(raw.SQLITE_CONFIG_URI, 1) != raw.SQLITE_OK)
                {
                    throw new SQLiteException("Unable to configure sqlite3 for URI connection strings.");
                }

                sqliteIsInitialized = true;
            }

            int rc = raw.sqlite3_open(connectionString, out connection);
            if (rc != raw.SQLITE_OK)
            {
                throw new SQLiteException("Unable to open database connection to '{connectionString}'", rc, connection);
            }

            int limit = raw.sqlite3_limit(connection, raw.SQLITE_LIMIT_VARIABLE_NUMBER, -1);
            MaxParametersPerQuery = limit - 16;
        }

        /// <summary>
        /// Prepares a SQL statement for use.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement to prepare.</param>
        /// <returns>A <see cref="SqliteStatement"/>.</returns>
        public SqliteStatement PrepareStatement(string sqlStatement)
        {
            Arguments.IsNotNullOrWhitespace(sqlStatement, nameof(sqlStatement));

            int rc = raw.sqlite3_prepare_v2(connection, sqlStatement, out sqlite3_stmt stmt);
            if (rc != raw.SQLITE_OK)
            {
                throw new SQLiteException($"Cannot prepare statement for '{sqlStatement}'", rc, connection);
            }

            return new SqliteStatement(connection, stmt);
        }
    }
}
