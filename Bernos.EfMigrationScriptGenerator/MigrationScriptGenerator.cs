using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bernos.EfMigrationScriptGenerator
{
    public class MigrationScriptGenerator
    {
        private readonly Assembly _migrationAssembly;
        private readonly Assembly _entityFrameworkAssembly;
        private readonly string _connectionString;
        private readonly string _providerName;
        public MigrationScriptGenerator(Assembly migrationAssembly, string connectionString, string providerName)
        {
            _migrationAssembly = migrationAssembly;
            _connectionString = connectionString;
            _providerName = providerName;
            
            var efAssemblyName = migrationAssembly.GetReferencedAssemblies().FirstOrDefault(a => a.Name == "EntityFramework");

            if (efAssemblyName == null)
            {
                throw new Exception("Could not find a reference to the EntityFramework assembly through the provided migration assembly.");
            }
            else
            {
                _entityFrameworkAssembly = Assembly.Load(efAssemblyName);    
            }
        }

        public string GenerateMigrationSql()
        {
            var configuration = GetMigrationConfiguration(_entityFrameworkAssembly, _migrationAssembly, _connectionString, _providerName);
            var migrator = GetMigrator(_entityFrameworkAssembly, configuration);
            
            var allMigrations = migrator.GetLocalMigrations().ToArray();
            var pendingMigrations = migrator.GetPendingMigrations().ToArray();
            var sourceMigration = "";

            if (pendingMigrations.Length > 0)
            {
                var targetMigration = pendingMigrations[pendingMigrations.Length - 1];

                for (var i = 0; i < allMigrations.Length; i++)
                {
                    if (allMigrations[i] == pendingMigrations[0] && i > 0)
                    {
                        sourceMigration = allMigrations[i - 1];
                    }
                }

                var scriptor = Activator.CreateInstance(_entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.Infrastructure.MigratorScriptingDecorator"), migrator);

                var sb = new StringBuilder();
                sb.AppendLine(string.Format("-- Migration script generated {0}", DateTime.Now));
                sb.AppendLine(string.Format("-- Source Migration: {0}", sourceMigration));
                sb.AppendLine(string.Format("-- Target Migration: {0}", targetMigration));
                sb.AppendLine("");
                sb.AppendLine(scriptor.ScriptUpdate(sourceMigration, targetMigration));
                return sb.ToString();
            }

            return "-- No pending migrations to run. Db is up to date";
        }

        private dynamic GetMigrationConfiguration(Assembly entityFrameworkAssembly, Assembly migrationAssembly, string connectionString, string providerName)
        {
            var dbMigrationsConfigurationType =
                entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.DbMigrationsConfiguration");

            foreach (var type in migrationAssembly.GetTypes())
            {
                if (type.IsSubclassOf(dbMigrationsConfigurationType))
                {
                    var configuration = (dynamic)Activator.CreateInstance(type);

                    configuration.TargetDatabase =
                    (dynamic)
                        Activator.CreateInstance(
                            entityFrameworkAssembly.GetType("System.Data.Entity.Infrastructure.DbConnectionInfo"),
                            connectionString, providerName);

                    return configuration;
                }
            }

            return null;
        }

        private dynamic GetMigrator(Assembly entityFrameworkAssembly, dynamic migrationConfiguration)
        {
            var type = entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.DbMigrator");

            var instance = Activator.CreateInstance(type, migrationConfiguration);

            return instance;
        }
    }
}