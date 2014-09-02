using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bernos.EfMigrationScriptGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: read from std in
            var assemblyName = "Vino.Infrastructure.dll";

            // TODO: read from std in
            var connectionString = "Data Source=(localdb)\\v11.0;Initial Catalog=MigrationTest;Integrated Security=True";

            // TODO: read from std in
            var providerName = "System.Data.SqlClient";
            
            var migrationAssembly = Assembly.LoadFrom(assemblyName);
            var efAssemblyName =
                migrationAssembly.GetReferencedAssemblies().FirstOrDefault(a => a.Name == "EntityFramework");

            if (efAssemblyName != null)
            {
                var efAssembly = Assembly.Load(efAssemblyName);

                var configuration = GetMigrationsConfigurationDynamically(migrationAssembly, efAssembly);
                configuration.TargetDatabase =
                    (dynamic)
                        Activator.CreateInstance(
                            efAssembly.GetType("System.Data.Entity.Infrastructure.DbConnectionInfo"),
                            connectionString, providerName);

                var migrator = Activator.CreateInstance(efAssembly.GetType("System.Data.Entity.Migrations.DbMigrator"),
                    configuration);

                GenerateScriptDynamically(migrator, efAssembly);
            }
            else
            {
                throw new Exception("Could not find referenced entity framework assembly!");
            }
        }

        static void GenerateScriptDynamically(dynamic migrator, Assembly efAssembly)
        {
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
                
                var scriptor = Activator.CreateInstance(efAssembly.GetType("System.Data.Entity.Migrations.Infrastructure.MigratorScriptingDecorator"), migrator);

                var sql = scriptor.ScriptUpdate(sourceMigration, targetMigration);

                if (!string.IsNullOrEmpty(sql))
                {
                    Console.WriteLine(sql);
                }
                else
                {

                }
            }
            else
            {
                Console.WriteLine("-- No pending migrations to run. Db is up to date");
            }

            Console.WriteLine("-- DONE");
        }

        /*
        static void GenerateScript(dynamic migrator)
        {
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

                var scriptor = new MigratorScriptingDecorator(migrator);
                var sql = scriptor.ScriptUpdate(sourceMigration, targetMigration);

                if (!string.IsNullOrEmpty(sql))
                {
                    Console.WriteLine(sql);
                }
                else
                {

                }
            }
            else
            {
                Console.WriteLine("-- No pending migrations to run. Db is up to date");
            }

            Console.WriteLine("-- DONE");
        }
        */
        /*
        static void GenerateScript(DbMigrator migrator)
        {
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

                var scriptor = new MigratorScriptingDecorator(migrator);
                var sql = scriptor.ScriptUpdate(sourceMigration, targetMigration);

                if (!string.IsNullOrEmpty(sql))
                {
                    Console.WriteLine(sql);
                }
                else
                {

                }
            }
            else
            {
                Console.WriteLine("-- No pending migrations to run. Db is up to date");
            }

            Console.WriteLine("-- DONE");
        }
        */

        private static dynamic GetMigrationsConfigurationDynamically(Assembly assembly, Assembly efAssembly)
        {
            var dbMigrationsConfigurationType =
                efAssembly.GetType("System.Data.Entity.Migrations.DbMigrationsConfiguration");

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(dbMigrationsConfigurationType))
                {
                    return Activator.CreateInstance(type);
                }
            }

            return null;
        }

        /*
        private static DbMigrationsConfiguration GetMigrationsConfiguration(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof (DbMigrationsConfiguration)))
                {
                    return GetMigrationsConfiguration(assembly, type.FullName);
                }
            }

            return null;
        }

        private static DbMigrationsConfiguration GetMigrationsConfiguration(Assembly assembly, string configurationType)
        {
            var type = assembly.GetType(configurationType);

            return (DbMigrationsConfiguration) Activator.CreateInstance(type);
        }
         * */
    }
}
