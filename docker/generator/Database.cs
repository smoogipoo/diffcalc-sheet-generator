// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MySqlConnector;

namespace Generator;

public static class Database
{
    public static async Task<MySqlConnection> GetConnection()
    {
        string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? String.Empty;

        if (string.IsNullOrEmpty(connectionString))
        {
            string host = (Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost");
            string user = (Environment.GetEnvironmentVariable("DB_USER") ?? "root");
            string password = (Environment.GetEnvironmentVariable("DB_PASS") ?? string.Empty);

            string passwordString = string.IsNullOrEmpty(password) ? string.Empty : $"Password={password};";

            connectionString = $"Server={host};User ID={user};{passwordString}ConnectionTimeout=5;ConnectionReset=false;Pooling=true;";
        }

        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
