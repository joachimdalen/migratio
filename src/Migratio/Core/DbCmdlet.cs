using System;
using System.Management.Automation;
using Migratio.Database;

namespace Migratio.Core
{
    public abstract class DbCmdlet : BaseCmdlet
    {
        public DbCmdlet() : base()
        {
        }

        public DbCmdlet(CmdletDependencies dependencies) : base(dependencies)
        {
        }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Username of database user")
        ]
        [ValidateNotNullOrEmpty]
        public string Username { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Specifies the name of the database to connect to")
        ]
        [ValidateNotNullOrEmpty]
        public string Database { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Specifies the port on the database server to connect to")
        ]
        [ValidateNotNullOrEmpty]
        public int Port { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Specifies the hostname or ip address of the machine to connect to")
        ]
        [ValidateNotNullOrEmpty]
        public string Host { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Specifies the default database schema. Only valid for Postgres")
        ]
        [ValidateNotNullOrEmpty]
        public string Schema { get; set; }

        /// <summary>
        /// Get database connection info.
        /// </summary>
        /// <returns></returns>
        protected DbConnectionInfo GetConnectionInfo()
        {
            var dbInfo = Configuration?.Config?.Auth?.Postgres;
            return new DbConnectionInfo
            {
                Database = Database ?? ResolveDynamicConfig<string, string>(dbInfo?.Database, null, null),
                Username = Username ?? ResolveDynamicConfig<string, string>(dbInfo?.Username, null, null),
                Host = Host ?? ResolveDynamicConfig<string, string>(dbInfo?.Host, null, "127.0.0.1"),
                Password = ResolveDynamicConfig<string, string>(dbInfo?.Password, "MG_DB_PASSWORD", null),
                Port = Port == 0 ? ResolveDynamicConfig<int?, int>(dbInfo?.Port, null, 5432) : Port,
                Schema = Schema ?? ResolveDynamicConfig<string, string>(dbInfo?.Schema, null, "public")
            };
        }

        private T2 ResolveDynamicConfig<T, T2>(T fromConfig, string envKey, T defaultValue)
        {
            var value = ResolveConfig(fromConfig?.ToString(), envKey, defaultValue?.ToString());

            if (typeof(T2) == typeof(string)) return (T2) (object) value;

            if (typeof(T2) == typeof(bool))
            {
                bool.TryParse(value, out var boolRes);
                return (T2) (object) boolRes;
            }

            if (typeof(T2) == typeof(int))
            {
                int.TryParse(value, out var intRes);
                return (T2) (object) intRes;
            }

            throw new Exception("Unsupported configuration type");
        }

        private string ResolveConfig(string fromConfig, string envKey, string defaultValue)
        {
            if (fromConfig != null)
            {
                if (SecretManager.HasVariable(fromConfig)) return SecretManager.ReplaceVariablesInContent(fromConfig);

                return fromConfig;
            }

            if (!string.IsNullOrEmpty(envKey)) return SecretManager.GetEnvironmentVariable(envKey);

            return defaultValue;
        }
    }
}