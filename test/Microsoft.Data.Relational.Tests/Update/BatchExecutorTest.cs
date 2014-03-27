﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Relational.Update;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.Data.Relational.Model;

namespace Microsoft.Data.Relational.Tests.Update
{
    public class BatchExecutorTest
    {
        [Fact]
        public async void ExecuteAsync_executes_batch_commands_and_consumes_reader()
        {
            var mockModificationCommand = new Mock<ModificationCommand>();
            mockModificationCommand.Setup(c => c.Table).Returns(new Table("table"));
            mockModificationCommand.Setup(c => c.ColumnValues)
                .Returns(new[] { new KeyValuePair<Column, object>(new Column("Id", "_"), 1) });
            mockModificationCommand.Setup(c => c.WhereClauses).Returns(new KeyValuePair<Column, object>[0]);

            var batch = new ModificationCommandBatch(new[] { mockModificationCommand.Object });

            var executor = new BatchExecutor(new[] { batch }, new Mock<SqlGenerator> { CallBase = true }.Object);

            var connection = SetupMockConnection(1);

            await executor.ExecuteAsync(connection, CancellationToken.None);

            var mockReader = Mock.Get(await connection.CreateCommand().ExecuteReaderAsync());

            mockReader
                .Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);

            mockReader
                .Verify(r => r.NextResultAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void ExecuteAsync_throws_if_records_affected_and_command_count_dont_match()
        {
            var mockModificationCommand = new Mock<ModificationCommand>();
            mockModificationCommand.Setup(c => c.Table).Returns(new Table("table"));
            mockModificationCommand.Setup(c => c.ColumnValues)
                .Returns(new[] { new KeyValuePair<Column, object>(new Column("Id", "_"), 1) });
            mockModificationCommand.Setup(c => c.WhereClauses).Returns(new KeyValuePair<Column, object>[0]);

            var batch = new ModificationCommandBatch(new[] { mockModificationCommand.Object });

            var executor = new BatchExecutor(new[] { batch }, new Mock<SqlGenerator> { CallBase = true }.Object);

            Assert.Equal(
                Strings.FormatUpdateConcurrencyException(1, 0),
                (await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
                    await executor.ExecuteAsync(SetupMockConnection(0), CancellationToken.None))).Message);
        }

        private DbConnection SetupMockConnection(int recordsAffected)
        {
            var mockConnection = new Mock<DbConnection>();
            var mockCommand = new Mock<DbCommand>();
            mockConnection
                .Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(mockCommand.Object);
            mockCommand
                .Protected()
                .Setup<DbParameter>("CreateDbParameter")
                .Returns(Mock.Of<DbParameter>());
            mockCommand
                .Protected()
                .SetupGet<DbParameterCollection>("DbParameterCollection")
                .Returns(Mock.Of<DbParameterCollection>());

            var mockDataReader = new Mock<DbDataReader>();
            mockDataReader
                .Setup(r => r.RecordsAffected)
                .Returns(recordsAffected);
            var tcs = new TaskCompletionSource<DbDataReader>();
            tcs.SetResult(mockDataReader.Object);

            mockCommand
                .Protected()
                .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", ItExpr.IsAny<CommandBehavior>(), ItExpr.IsAny<CancellationToken>())
                .Returns(tcs.Task);

            return mockConnection.Object;
        }
    }
}