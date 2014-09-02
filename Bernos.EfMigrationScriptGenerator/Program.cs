using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NDesk.Options;

namespace Bernos.EfMigrationScriptGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var assemblyName = "";
            var connectionString = "";
            var showHelp = false;
            var providerName = "System.Data.SqlClient";
            var targetMigration = "";
            var outputFile = "";

            var p = new OptionSet
            {
                {"a|assembly=", "The name of the assembly containing your migrations.", v => assemblyName = v},
                {"c|connectionString=", "The connection string to use", v => connectionString = v},
                {"t|targetMigration=", "The name of the migration to migrate to. Defaults to the newest migration found in your assembly.", v => targetMigration = v},
                {"o|output=", "File to output the script to. Defaults to std out", v => outputFile = v},
                {"p|providerName=", "The provider type to use. Defaults to System.Data.SqlClient", v =>
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        providerName = v;
                    }
                }},
                {"h|help", "Show this message", v => showHelp = true}
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                ShowError(e.Message);
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (string.IsNullOrEmpty(assemblyName))
            {
                ShowError("Assembly name cannot be empty.");
                return;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                ShowError("Connection string cannot be empty");
                return;
            }

            var migrationAssembly = Assembly.LoadFrom(assemblyName);
            var migrationScriptGenerator = new MigrationScriptGenerator(migrationAssembly, connectionString, providerName);
            var sql = migrationScriptGenerator.GenerateMigrationSql(targetMigration);

            if (!string.IsNullOrEmpty(outputFile))
            {
                File.WriteAllText(outputFile, sql);
            }
            else
            {
                Console.WriteLine(sql);    
            }
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: Bernos.EfMigrationScriptGenerator.exe [OPTIONS]+ message");
            Console.WriteLine("Generate sql script from Entity Framework code-first migrations.");
            Console.WriteLine("If no message is specified, a generic greeting is used.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void ShowError(string error)
        {
            Console.Write(error);
            Console.Write("Try --help for usage");
        }
    }
}
