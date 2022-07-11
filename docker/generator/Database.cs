// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MySqlConnector;

namespace Generator;

public static class Database
{
    public static async Task<MySqlConnection> GetConnection()
    {
        var connection = new MySqlConnection("Server=db;User ID=root;ConnectionTimeout=5;ConnectionReset=false;Pooling=true;");
        await connection.OpenAsync();
        return connection;
    }
}
