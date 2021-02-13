using System;
using System.Collections.Generic;
using System.Linq;
using Migratio.Contracts;
using Migratio.Models;
using Moq;
using Xunit;

namespace Migratio.UnitTests
{
    public class GetMigratioProcessedMigrationsTests
    {
        [Fact(DisplayName = "Get-MigratioProcessedMigrations returns records")]
        public void MigratioProcessedMigrations_Returns_Records()
        {
            var dbMock = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            dbMock.Setup(x => x.MigrationTableExists()).Returns(true);
            dbMock.Setup(x => x.GetAppliedMigrations()).Returns(
                new List<Migration>
                {
                    new() {Iteration = 1, MigrationId = "0001_migration_1"},
                    new() {Iteration = 1, MigrationId = "0002_migration_2"},
                }.ToArray());
            var command = new GetMigratioProcessedMigrations(dbMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            var result = command.Invoke()?.OfType<Migration[]>()?.First();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Length);
        }

        [Fact(DisplayName = "Get-MigratioProcessedMigrations throws if migration table does not exist")]
        public void MigratioProcessedMigrations_Throws_If_Migration_Table_Does_Not_Exist()
        {
            var dbMock = new Mock<IDatabaseProvider>(MockBehavior.Strict);
            dbMock.Setup(x => x.MigrationTableExists()).Returns(false);

            var command = new GetMigratioProcessedMigrations(dbMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            Assert.Throws<Exception>(() => command.Invoke()?.OfType<Migration[]>()?.First());
        }

        [Fact(DisplayName = "Get-MigratioProcessedMigrations default constructor constructs")]
        public void GetMigratioProcessedMigrations_Default_Constructor_Constructs()
        {
            var result = new GetMigratioProcessedMigrations
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            Assert.NotNull(result);
        }
    }
}