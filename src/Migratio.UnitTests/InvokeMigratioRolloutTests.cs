using System;
using System.Linq;
using Migratio.Contracts;
using Migratio.Models;
using Migratio.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Migratio.UnitTests
{
    public class InvokeMigratioRolloutTests
    {
        private readonly DatabaseProviderMock _dbMock;
        private readonly FileManagerMock _fileManagerMock;
        private readonly EnvironmentManagerMock _envMock;

        public InvokeMigratioRolloutTests()
        {
            _dbMock = new DatabaseProviderMock();
            _fileManagerMock = new FileManagerMock();
            _envMock = new EnvironmentManagerMock();
        }

        [Fact(DisplayName = "Invoke-MigratioRollout throws if migration table does not exist")]
        public void InvokeMigratioRollout_Throws_If_Migration_Table_Does_Not_Exist()
        {
            _dbMock.MigrationTableExists(false);

            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            Assert.Throws<Exception>(() => command.Invoke()?.OfType<bool>()?.First());
        }

        [Fact(DisplayName = "Invoke-MigratioRollout returns if no scripts found")]
        public void InvokeMigratioRollout_Returns_If_No_Scripts_Found()
        {
            _dbMock.MigrationTableExists(true);
            _fileManagerMock.GetAllFilesInFolder(Array.Empty<string>());
            _fileManagerMock.RolloutDirectory("mig/rol");

            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            var result = command.Invoke()?.OfType<bool>()?.First();
            Assert.False(result);
        }

        [Fact(DisplayName = "Invoke-MigratioRollout returns if all scripts are applied")]
        public void InvokeMigratioRollout_Returns_If_All_Scripts_Are_Applied()
        {
            _dbMock.MigrationTableExists(true);
            _fileManagerMock.RolloutDirectory("mig/rol");
            _fileManagerMock.GetAllFilesInFolder(new[] {"migrations/one.sql", "migrations/two.sql"});
            _dbMock.GetAppliedMigrations(new Migration[]
            {
                new() {Iteration = 1, MigrationId = "one.sql"},
                new() {Iteration = 1, MigrationId = "two.sql"}
            });


            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username"
            };

            var result = command.Invoke()?.OfType<string>()?.ToArray();
            Assert.Equal("Number of applied migrations are the same as the total, skipping", result[2]);
        }

        [Fact(DisplayName = "Invoke-MigratioRollout skips migration if applied")]
        public void InvokeMigratioRollout_Skips_Migration_If_Applied()
        {
            _dbMock.MigrationTableExists(true);
            _dbMock.GetLatestIteration(1);

            _fileManagerMock.RolloutDirectory("migrations/rollout");
            _fileManagerMock.GetAllFilesInFolder(new[] {"migrations/rollout/one.sql", "migrations/rollout/two.sql"});
            _fileManagerMock.ReadAllText("migrations/rollout/two.sql", "SELECT 1 from 2");

            _dbMock.RunTransactionAny(1);
            _dbMock.GetAppliedMigrations(new Migration[]
            {
                new() {Iteration = 1, MigrationId = "one"}
            });


            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.MockInstance.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username",
                AsSingleMigrations = true,
            };

            var result = command.Invoke()?.OfType<string>()?.ToArray();
            Assert.Equal("Migration one is applied, skipping", result[2]);

            _fileManagerMock.VerifyReadAllText("migrations/rollout/one.sql", Times.Never());
        }

        [Fact(DisplayName = "Invoke-MigratioRollout runs as single transactions if true")]
        public void InvokeMigratioRollout_Runs_As_Single_Transactions_If_True()
        {
            _dbMock.MigrationTableExists(true);
            _dbMock.GetLatestIteration(1);

            _fileManagerMock.RolloutDirectory("migrations/rollout");
            _fileManagerMock.GetAllFilesInFolder(new[]
            {
                "migrations/rollout/one.sql",
                "migrations/rollout/two.sql",
                "migrations/rollout/three.sql"
            });
            _fileManagerMock.ReadAllText("migrations/rollout/two.sql", "SELECT 1 from 2");
            _fileManagerMock.ReadAllText("migrations/rollout/three.sql", "SELECT 3 from 4");

            _dbMock.RunTransactionAny(1);
            _dbMock.GetAppliedMigrations(new Migration[]
            {
                new() {Iteration = 1, MigrationId = "one"}
            });


            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.MockInstance.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username",
                AsSingleMigrations = true,
            };

            var result = command.Invoke()?.OfType<string>()?.ToArray();

            Assert.Contains("Running migration two", result);
            Assert.Contains("Running migration three", result);
            _fileManagerMock.VerifyReadAllText("migrations/rollout/one.sql", Times.Never());
            _dbMock.VerifyRunTransaction(
                "SELECT 3 from 4;INSERT INTO \"public\".\"MIGRATIONS\" (\"MIGRATION_ID\", \"ITERATION\") VALUES ('three', 2);");
            _dbMock.VerifyRunTransaction(
                "SELECT 1 from 2;INSERT INTO \"public\".\"MIGRATIONS\" (\"MIGRATION_ID\", \"ITERATION\") VALUES ('two', 3);");
        }

        [Fact(DisplayName = "Invoke-MigratioRollout should replace variables")]
        public void InvokeMigratioRollout_Should_Replace_Variables()
        {
            _dbMock.MigrationTableExists(true);
            _dbMock.GetLatestIteration(1);

            _fileManagerMock.RolloutDirectory("migrations/rollout");
            _fileManagerMock.GetAllFilesInFolder(new[]
            {
                "migrations/rollout/one.sql",
            });
            _envMock.GetEnvironmentVariable("TEST_ITEM_VARIABLE", "ReplacedValue");
            _fileManagerMock.ReadAllText("migrations/rollout/one.sql", "SELECT 1 from '${{TEST_ITEM_VARIABLE}}'");

            _dbMock.RunTransactionAny(1);
            _dbMock.GetAppliedMigrations(Array.Empty<Migration>());


            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.MockInstance.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username",
                AsSingleMigrations = true,
                ReplaceVariables = true
            };

            var result = command.Invoke()?.OfType<string>()?.ToArray();

            Assert.Contains("Running migration one", result);
            _dbMock.VerifyRunTransaction(
                "SELECT 1 from 'ReplacedValue';INSERT INTO \"public\".\"MIGRATIONS\" (\"MIGRATION_ID\", \"ITERATION\") VALUES ('one', 2);");
        }

        [Fact(DisplayName = "Invoke-MigratioRollout runs as one transaction if false")]
        public void InvokeMigratioRollout_Runs_As_One_Transaction_If_False()
        {
            _dbMock.MigrationTableExists(true);
            _dbMock.GetLatestIteration(1);

            _fileManagerMock.RolloutDirectory("migrations/rollout");
            _fileManagerMock.GetAllFilesInFolder(new[]
            {
                "migrations/rollout/one.sql",
                "migrations/rollout/two.sql",
                "migrations/rollout/three.sql"
            });
            _fileManagerMock.ReadAllText("migrations/rollout/two.sql", "SELECT 1 from 2");
            _fileManagerMock.ReadAllText("migrations/rollout/three.sql", "SELECT 3 from 4");

            _dbMock.RunTransactionAny(1);
            _dbMock.GetAppliedMigrations(new Migration[]
            {
                new() {Iteration = 1, MigrationId = "one"}
            });


            var command = new InvokeMigratioRollout(_dbMock.Object, _fileManagerMock.MockInstance.Object, _envMock.Object)
            {
                Database = "database",
                Password = "password",
                Host = "host",
                Port = 1111,
                Schema = "public",
                Username = "username",
                AsSingleMigrations = false
            };

            var result = command.Invoke()?.OfType<string>()?.ToArray();

            Assert.Contains("Migration two is not applied adding to transaction", result);
            Assert.Contains("Migration three is not applied adding to transaction", result);
            _fileManagerMock.VerifyReadAllText("migrations/rollout/one.sql", Times.Never());
            _dbMock.VerifyRunTransaction(
                "SELECT 3 from 4;INSERT INTO \"public\".\"MIGRATIONS\" (\"MIGRATION_ID\", \"ITERATION\") VALUES ('three', 2);SELECT 1 from 2;INSERT INTO \"public\".\"MIGRATIONS\" (\"MIGRATION_ID\", \"ITERATION\") VALUES ('two', 2);");
        }

        [Fact(DisplayName = "Invoke-MigratioRollout default constructor constructs")]
        public void InvokeMigratioRollout_Default_Constructor_Constructs()
        {
            var result = new InvokeMigratioRollout
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