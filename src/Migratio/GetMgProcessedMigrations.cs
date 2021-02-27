using System;
using System.Management.Automation;
using Migratio.Core;
using Migratio.Models;

namespace Migratio
{
    [Cmdlet(VerbsCommon.Get, "MgProcessedMigrations")]
    [OutputType(typeof(Migration[]))]
    public class GetMgProcessedMigrations : DbCmdlet
    {
        public GetMgProcessedMigrations()
        {
        }

        public GetMgProcessedMigrations(CmdletDependencies dependencies) : base(dependencies)
        {
        }

        protected override void ProcessRecord()
        {
            DatabaseProvider.SetConnectionInfo(GetConnectionInfo());

            if (!DatabaseProvider.MigrationTableExists()) throw new Exception("Migration table does not exist");

            var processed = DatabaseProvider.GetAppliedMigrations();

            WriteObject(processed);
        }
    }
}