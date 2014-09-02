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
            var migrationScriptGenerator = new MigrationScriptGenerator(migrationAssembly, connectionString, providerName);

            Console.WriteLine(migrationScriptGenerator.GenerateMigrationSql());
        }
    }
}
