using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Swapping.Api.Data;

namespace Swapping.Api.Tests;

/// <summary>Reads the raw SQLite database in tests (tables, columns, every stored value as text).</summary>
public static class DatabaseDump
{
    public static List<string> Tables(GameDbContext db)
    {
        using var command = Open(db).CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        using var reader = command.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read())
            tables.Add(reader.GetString(0));
        return tables;
    }

    public static List<string> AllColumns(GameDbContext db)
    {
        var columns = new List<string>();
        foreach (var table in Tables(db))
        {
            using var command = Open(db).CreateCommand();
            command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(0));
        }
        return columns;
    }

    /// <summary>Every value of every row of every table, as text (blobs as UTF-8 and as hex).</summary>
    public static List<string> AllValues(GameDbContext db)
    {
        var values = new List<string>();
        foreach (var table in Tables(db))
        {
            using var command = Open(db).CreateCommand();
            command.CommandText = $"SELECT * FROM \"{table}\"";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    switch (reader.GetValue(i))
                    {
                        case byte[] blob:
                            values.Add(Encoding.UTF8.GetString(blob));
                            values.Add(Convert.ToHexString(blob));
                            break;
                        case DBNull:
                            break;
                        case var value:
                            values.Add(Convert.ToString(value) ?? "");
                            break;
                    }
                }
            }
        }
        return values;
    }

    private static DbConnection Open(GameDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();
        return connection;
    }
}
