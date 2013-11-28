using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtextJekyllExporter
{
    internal class Program
    {
        private const string postFormat = @"---
layout: {0}
title: ""{1}""
date: {2}
comments: true
categories: {3}
---
{4}
";
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
                        string content = FormatCode(EscapeJekyllTags(ConvertHtmlToMarkdown(reader.GetString(1))));
                        string layout = reader.GetString(2);
                        string title = reader.GetString(3);
                        string date = reader.GetString(4);
                        string categories = reader.GetString(5);

                        string formattedContent = String.Format(postFormat, layout, title, date, categories, content);

                        var path = Path.Combine(rootDirectory, filePath);
                        EnsurePath(path);
                        Console.WriteLine("Writing: " + title);
                        File.WriteAllText(path, formattedContent, new UTF8Encoding(false));
                    }
                }
            }
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

        private static void EnsurePath(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        public static string ConvertHtmlToMarkdown(string source)
        {
            string args = String.Format(@"-r html -t markdown");

            var startInfo = new ProcessStartInfo("pandoc.exe", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var process = new Process {StartInfo = startInfo};
            process.Start();

            var inputBuffer = Encoding.UTF8.GetBytes(source);
            process.StandardInput.BaseStream.Write(inputBuffer, 0, inputBuffer.Length);
            process.StandardInput.Close();

            process.WaitForExit(2000);
            using (var sr = new StreamReader(process.StandardOutput.BaseStream))
            {
                return sr.ReadToEnd();
            }
        }

        private static string EscapeJekyllTags(string content)
        {
            return content
                .Replace("{{", "{{ \"{{\" }}")
                .Replace("{%", "{{ \"{%\" }}");
        }

        static readonly Regex _codeRegex = new Regex(@"~~~~ \{\.csharpcode\}(?<code>.*?)~~~~", RegexOptions.Compiled | RegexOptions.Singleline);

        private static string FormatCode(string content)
        {
            return _codeRegex.Replace(content, match =>
            {
                var code = match.Groups["code"].Value;
                return "```" + GetLanguage(code) + code + "```";
            });
        }

        private static string GetLanguage(string code)
        {
            var trimmedCode = code.Trim();
            if (trimmedCode.Contains("<%= ") || trimmedCode.Contains("<%: ")) return "aspx-cs";
            if (trimmedCode.StartsWith("<script") || trimmedCode.StartsWith("<table")) return "html";
            return "csharp";
        }
    }
}