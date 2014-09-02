using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
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


            var configuration = GetMigrationsConfiguration(Assembly.LoadFrom(assemblyName));
            configuration.TargetDatabase = new DbConnectionInfo(connectionString, providerName);

            var migrator = new DbMigrator(configuration);

            GenerateScript(migrator);
        }

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
    }
}
