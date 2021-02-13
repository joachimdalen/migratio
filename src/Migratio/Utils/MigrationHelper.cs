using System;
using Migratio.Contracts;
using Migratio.Secrets;

namespace Migratio.Utils
{
    public class MigrationHelper
    {
        private readonly IFileManager _fileManager;
        private readonly SecretManager _secretManager;

        public MigrationHelper(IFileManager fileManager, IEnvironmentManager environmentManager)
        {
            _fileManager = fileManager;
            _secretManager = new SecretManager(environmentManager);
        }

        public string GetScriptContent(string scriptPath, bool replace)
        {
            var scriptContent = _fileManager.ReadAllText(scriptPath);
            if (!scriptContent.EndsWith(";"))
                scriptContent += ";";

            if (!replace) return scriptContent + Environment.NewLine;

            var replacedContent = _secretManager.ReplaceSecretsInContent(scriptContent);
            return replacedContent + Environment.NewLine;
        }
    }
}