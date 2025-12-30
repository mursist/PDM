using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BomEtlAddin.Models;

namespace BomEtlAddin.Utilities
{
    public static class SqlBomWriter
    {
        public static void Write(string connectionString, string documentPath, IReadOnlyCollection<BomRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                EnsureTable(connection);

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var row in rows)
                    {
                        InsertRow(connection, transaction, documentPath, row);
                    }

                    transaction.Commit();
                }
            }
        }

        private static void EnsureTable(SqlConnection connection)
        {
            var commandText = @"
IF OBJECT_ID(N'dbo.BomItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BomItems
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentPath NVARCHAR(500) NOT NULL,
        ItemNumber NVARCHAR(50) NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        Configuration NVARCHAR(100) NULL,
        Quantity INT NOT NULL,
        ExportedAtUtc DATETIME2 NOT NULL
    );
END";

            using (var command = new SqlCommand(commandText, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void InsertRow(SqlConnection connection, SqlTransaction transaction, string documentPath, BomRow row)
        {
            const string insertCommand = @"
INSERT INTO dbo.BomItems
    (DocumentPath, ItemNumber, PartNumber, Description, Configuration, Quantity, ExportedAtUtc)
VALUES
    (@DocumentPath, @ItemNumber, @PartNumber, @Description, @Configuration, @Quantity, @ExportedAtUtc);";

            using (var command = new SqlCommand(insertCommand, connection, transaction))
            {
                command.Parameters.Add("@DocumentPath", SqlDbType.NVarChar, 500).Value = documentPath ?? string.Empty;
                command.Parameters.Add("@ItemNumber", SqlDbType.NVarChar, 50).Value = row.ItemNumber ?? string.Empty;
                command.Parameters.Add("@PartNumber", SqlDbType.NVarChar, 100).Value = row.PartNumber ?? string.Empty;
                command.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = row.Description ?? string.Empty;
                command.Parameters.Add("@Configuration", SqlDbType.NVarChar, 100).Value = row.Configuration ?? string.Empty;
                command.Parameters.Add("@Quantity", SqlDbType.Int).Value = row.Quantity;
                command.Parameters.Add("@ExportedAtUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;

                command.ExecuteNonQuery();
            }
        }
    }
}
