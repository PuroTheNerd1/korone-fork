using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Roblox.Services;

public static class Database
{
    private static string dbConnectionString { get; set; } = string.Empty;
    private static Mutex connectionMutex { get; } = new();
    // Default timeout of 5 seconds to prevent indefinite waiting
    private static readonly int mutexTimeoutMs = 5000;

    public static bool AcquireConnectionMutex(string debugReason)
    {
        try
        {
            return connectionMutex.WaitOne(mutexTimeoutMs);
        }
        catch (AbandonedMutexException)
        {
            // If mutex was abandoned, log it and consider it acquired
            Console.WriteLine($"Warning: Abandoned mutex detected in {debugReason}");
            return true;
        }
        catch (Exception ex)
        {
            // Log other exceptions but don't block execution
            Console.WriteLine($"Error acquiring mutex in {debugReason}: {ex.Message}");
            return false;
        }
    }

    public static void ReleaseConnectionMutex()
    {
        try
        {
            connectionMutex.ReleaseMutex();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error releasing mutex: {ex.Message}");
        }
    }

    public static NpgsqlConnection connection
    {
        get
        {
            bool acquired = AcquireConnectionMutex("Database.connection");
            try
            {
                var myConn = unsafeConnection;
                return myConn;
            }
            finally
            {
                if (acquired)
                {
                    ReleaseConnectionMutex();
                }
            }
        }
    }
    
    public static NpgsqlConnection unsafeConnection => new NpgsqlConnection(dbConnectionString);

    /// <summary>
    /// List of table names, use for preparing statements
    /// </summary>
    public static List<string> tableNames { get; set; } = new();

    /// <summary>
    /// A dictionary of tableName to tableColumns
    /// </summary>
    public static Dictionary<string, List<string>> tableToColumnMap { get; set; } = new();
    
    public static void Configure(string databaseConnectionString)
    {
        if (string.IsNullOrEmpty(databaseConnectionString))
        {
            throw new ArgumentNullException(nameof(databaseConnectionString));
        }

        dbConnectionString = databaseConnectionString;

        try
        {
            using var conn = connection;
            var allTables = conn.Query("select * from information_schema.tables WHERE table_schema = :table_schema AND table_catalog = :table_catalog", new
            {
                table_schema = "public", // todo: this will be configurable
                table_catalog = conn.Database,
            });
            
            tableNames.Clear();
            tableToColumnMap.Clear();
            
            foreach (var item in allTables)
            {
                var cols = conn.Query("SELECT * FROM information_schema.columns WHERE table_schema = :schema AND table_name = :table AND table_catalog = :catalog", new
                {
                    catalog = conn.Database,
                    table = item.table_name,
                    schema = "public",
                });
                tableNames.Add(item.table_name);
                tableToColumnMap.Add(item.table_name, new List<string>());
                foreach (var col in cols)
                {
                    tableToColumnMap[item.table_name].Add(col.column_name);
                }
            }
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error configuring database: {ex.Message}");
            throw;
        }
    }
}
