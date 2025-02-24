﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Datasync.Common.Test.Models;
using Microsoft.Datasync.Client.Offline;
using Microsoft.Datasync.Client.Offline.Queue;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Datasync.Client.Test.Table.Operations.OfflineTable
{
    [ExcludeFromCodeCoverage]
    public class PurgeItemsAsync_Tests : BaseOperationTest
    {
        private readonly Random rnd = new();

        private readonly IdEntity testObject;
        private readonly JObject jsonObject;

        public PurgeItemsAsync_Tests() : base()
        {
            testObject = new IdEntity { Id = Guid.NewGuid().ToString("N"), StringValue = "testValue" };
            jsonObject = (JObject)table.ServiceClient.Serializer.Serialize(testObject);
        }

        #region Helpers
        /// <summary>
        /// Creates some random operations in the queue and returns the number of operations created.
        /// </summary>
        /// <param name="tableName">The name of the table holding the operations.</param>
        /// <param name="itemCount">The number of items to create.</param>
        /// <returns>The list of items created.</returns>
        private List<TableOperation> AddRandomOperations(string tableName, int itemCount = 0)
        {
            if (itemCount <= 0)
            {
                itemCount = rnd.Next(1, 20);
            }
            List<TableOperation> operations = new();
            for (var i = 0; i < itemCount; i++)
            {
                int t = rnd.Next(1, 4);
                switch (t)
                {
                    case 1:
                        operations.Add(new DeleteOperation(tableName, Guid.NewGuid().ToString()) { Item = jsonObject });
                        break;
                    case 2:
                        operations.Add(new InsertOperation(tableName, Guid.NewGuid().ToString()));
                        break;
                    default:
                        operations.Add(new UpdateOperation(tableName, Guid.NewGuid().ToString()));
                        break;
                }
            }
            store.Upsert(SystemTables.OperationsQueue, operations.Select(op => op.Serialize()));
            return operations;
        }

        /// <summary>
        /// Creates some random records in a table
        /// </summary>
        /// <param name="tableName">The name of the table holding the records.</param>
        /// <param name="itemCount">The number of items to create.</param>
        /// <returns>The list of items created.</returns>
        private List<JObject> AddRandomRecords(string tableName, int itemCount = 0)
        {
            if (itemCount <= 0)
            {
                itemCount = rnd.Next(1, 20);
            }
            List<JObject> records = new();
            for (var i = 0; i < itemCount; i++)
            {
                var item = (JObject)jsonObject.DeepClone();
                item["id"] = Guid.NewGuid().ToString();
                records.Add(item);
            }
            store.Upsert(tableName, records);
            return records;
        }

        /// <summary>
        /// Stores a valid delta token in the offline store.
        /// </summary>
        /// <param name="qid">The query ID.</param>
        private void SetDeltaToken(string tableName, string qid)
        {
            var token = JObject.Parse($"{{\"id\":\"dt.{tableName}.{qid}\",\"value\":\"2021-03-12T03:30:04.000Z\"}}");
            store.Upsert(SystemTables.Configuration, new[] { token });
        }
        #endregion

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NullOptions()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();
            await Assert.ThrowsAsync<ArgumentNullException>(() => table.PurgeItemsAsync("", null));
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NullQuery()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();
            await Assert.ThrowsAsync<ArgumentNullException>(() => table.PurgeItemsAsync(null, new PurgeOptions()));
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoOptions_NoRecords()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();

            await table.PurgeItemsAsync("", new PurgeOptions());

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_Discard_NoRecords()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();
            var options = new PurgeOptions() { DiscardPendingOperations = true };

            await table.PurgeItemsAsync("", options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_Discard_QID_NoRecords()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();

            var options = new PurgeOptions() { DiscardPendingOperations = true, QueryId = "abc123" };

            await table.PurgeItemsAsync("", options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoDiscard_NoRecords()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();
            var options = new PurgeOptions() { DiscardPendingOperations = false };

            await table.PurgeItemsAsync("", options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoDiscard_QID_NoRecords()
        {
            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions() { DiscardPendingOperations = false, QueryId = "abc123" };

            await table.PurgeItemsAsync(query, options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoOptions_WithRecords()
        {
            AddRandomOperations("movies", 10);
            AddRandomRecords("movies", 10);
            SetDeltaToken("movies", "abc123");

            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions();

            await Assert.ThrowsAsync<InvalidOperationException>(() => table.PurgeItemsAsync(query, options));

            Assert.NotEmpty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.NotEmpty(store.TableMap[SystemTables.Configuration]["dt.movies.abc123"]);
            Assert.NotEmpty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_Discard_WithRecords()
        {
            AddRandomOperations("movies", 10);
            AddRandomRecords("movies", 10);
            SetDeltaToken("movies", "abc123");

            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions() { DiscardPendingOperations = true };

            await table.PurgeItemsAsync(query, options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.NotEmpty(store.TableMap[SystemTables.Configuration]["dt.movies.abc123"]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_Discard_QID_WithRecords()
        {
            AddRandomOperations("movies", 10);
            AddRandomRecords("movies", 10);
            SetDeltaToken("movies", "abc123");

            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions() { DiscardPendingOperations = true, QueryId = "abc123" };

            await table.PurgeItemsAsync(query, options);

            Assert.Empty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.Empty(store.TableMap[SystemTables.Configuration]);
            Assert.Empty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoDiscard_WithRecords()
        {
            AddRandomOperations("movies", 10);
            AddRandomRecords("movies", 10);
            SetDeltaToken("movies", "abc123");

            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions() { DiscardPendingOperations = false };

            await Assert.ThrowsAsync<InvalidOperationException>(() => table.PurgeItemsAsync(query, options));

            Assert.NotEmpty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.NotEmpty(store.TableMap[SystemTables.Configuration]["dt.movies.abc123"]);
            Assert.NotEmpty(store.TableMap["movies"]);
        }

        [Fact]
        [Trait("Method", "PurgeItemsAsync")]
        public async Task PurgeItems_NoDiscard_QID_WithRecords()
        {
            AddRandomOperations("movies", 10);
            AddRandomRecords("movies", 10);
            SetDeltaToken("movies", "abc123");

            await table.ServiceClient.InitializeOfflineStoreAsync();
            const string query = "";
            var options = new PurgeOptions() { DiscardPendingOperations = false, QueryId = "abc123" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => table.PurgeItemsAsync(query, options));

            Assert.NotEmpty(store.TableMap[SystemTables.OperationsQueue]);
            Assert.NotEmpty(store.TableMap[SystemTables.Configuration]["dt.movies.abc123"]);
            Assert.NotEmpty(store.TableMap["movies"]);
        }
    }
}
