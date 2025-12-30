using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BomEtlAddin.Models;

namespace BomEtlAddin.Utilities
{
    public static class CsvWriter
    {
        public static void Write(string path, IReadOnlyCollection<BomRow> rows)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine("Item,PartNumber,Description,Configuration,Quantity");

                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(",",
                        Escape(row.ItemNumber),
                        Escape(row.PartNumber),
                        Escape(row.Description),
                        Escape(row.Configuration),
                        row.Quantity.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private static string Escape(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
