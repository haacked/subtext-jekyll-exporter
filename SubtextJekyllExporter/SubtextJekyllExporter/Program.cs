using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;

namespace SubtextJekyllExporter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please pass a database name and an export directory.");
                return;
            }

            string databaseName = args[0];
            string rootDirectory = args[1];
            string connectionString =
            String.Format(@"Data Source=.\SQLEXPRESS;Initial Catalog={0};Integrated Security=True", databaseName);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(GetExportSqlScript(), connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string filePath = reader.GetString(0).Replace(Environment.NewLine, "");
                        string content = EscapeJekyllTags(reader.GetString(1));

                        var path = Path.Combine(rootDirectory, filePath);
                        EnsurePath(path);
                        File.WriteAllText(path, content, Encoding.UTF8);
                    }
                }
            }
        }

        private static string EscapeJekyllTags(string content)
        {
            return content
                .Replace("{{", "{{ \"{{\" }}")
                .Replace("{%", "{{ \"{%\" }}");
        }

        private static void EnsurePath(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        private static string GetExportSqlScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "SubtextJekyllExporter.select-content-for-jekyll.sql";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}