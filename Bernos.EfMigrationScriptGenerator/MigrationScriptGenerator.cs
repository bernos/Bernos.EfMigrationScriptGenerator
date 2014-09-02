using System;
using System.Data.SqlClient;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationAssembly"></param>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
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

        /// <summary>
        /// Returns sql statements necessary to update the database to the specified target migration
        /// </summary>
        /// <param name="targetMigration"></param>
        /// <returns></returns>
        public string GenerateMigrationSql(string targetMigration)
        {
            var configuration = GetMigrationConfiguration(_entityFrameworkAssembly, _migrationAssembly, _connectionString, _providerName);
            var migrator = GetMigrator(_entityFrameworkAssembly, configuration);
            var allMigrations = migrator.GetLocalMigrations().ToArray();
            var pendingMigrations = migrator.GetPendingMigrations().ToArray();
            var sourceMigration = "";

            if (pendingMigrations.Length > 0)
            {
                if (string.IsNullOrEmpty(targetMigration))
                {
                    targetMigration = pendingMigrations[pendingMigrations.Length - 1];
                }

                for (var i = 0; i < allMigrations.Length; i++)
                {
                    if (allMigrations[i] == pendingMigrations[0] && i > 0)
                    {
                        sourceMigration = allMigrations[i - 1];
                    }
                }

                var cs = new SqlConnectionStringBuilder(_connectionString);
                var scriptor = Activator.CreateInstance(_entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.Infrastructure.MigratorScriptingDecorator"), migrator);

                var sb = new StringBuilder();
                sb.AppendLine(string.Format("-- Migration script generated {0}", DateTime.Now));
                sb.AppendLine(string.Format("-- Server: {0}", cs.DataSource));
                sb.AppendLine(string.Format("-- Database: {0}", cs.InitialCatalog));
                sb.AppendLine(string.Format("-- Source Migration: {0}", sourceMigration));
                sb.AppendLine(string.Format("-- Target Migration: {0}", targetMigration));
                sb.AppendLine("");
                sb.AppendLine(scriptor.ScriptUpdate(sourceMigration, targetMigration));
                return sb.ToString();
            }

            return "-- No pending migrations to run. Db is up to date";
        }

        /// <summary>
        /// Locates and constructs an instance of a DbMigrationsConfiguration class found in the provided assembly. We
        /// return this dynamic to avoid linking against the entity framework dll directly. This means that users of 
        /// our .exe can 'BYO' the version of the entity framework dll that their own migration dll was compiled against
        /// </summary>
        /// <param name="entityFrameworkAssembly"></param>
        /// <param name="migrationAssembly"></param>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Constructs an instance of System.Data.Entity.Migrations.DbMigrator.  We
        /// return this dynamic to avoid linking against the entity framework dll directly. This means that users of 
        /// our .exe can 'BYO' the version of the entity framework dll that their own migration dll was compiled against
        /// </summary>
        /// <param name="entityFrameworkAssembly"></param>
        /// <param name="migrationConfiguration"></param>
        /// <returns></returns>
        private dynamic GetMigrator(Assembly entityFrameworkAssembly, dynamic migrationConfiguration)
        {
            var type = entityFrameworkAssembly.GetType("System.Data.Entity.Migrations.DbMigrator");

            var instance = Activator.CreateInstance(type, migrationConfiguration);

            return instance;
        }
    }
}