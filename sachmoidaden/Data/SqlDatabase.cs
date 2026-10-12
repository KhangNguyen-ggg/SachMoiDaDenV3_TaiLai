using System.Data;
using Microsoft.Data.SqlClient;

namespace sachmoidaden.Data;
public sealed class SqlRow : Dictionary<string, object?>
{
    public int Int(string key) => Convert.ToInt32(this[key]);
    public int? OptionalInt(string key) => this[key] is null ? null : Int(key);
    public long Long(string key) => Convert.ToInt64(this[key]);
    public string Text(string key) => Convert.ToString(this[key]) ?? "";
    public bool Bool(string key) => Convert.ToBoolean(this[key]);
    public DateTime Date(string key) => DateTime.SpecifyKind(Convert.ToDateTime(this[key]), DateTimeKind.Utc);
    public DateTime? OptionalDate(string key) => this[key] is null ? null : Date(key);
}

public sealed class SqlSession(SqlConnection connection, SqlTransaction? transaction = null)
{
    public async Task<List<List<SqlRow>>> BatchAsync(string sql, params (string Name, object? Value)[] values)
    {
        using var command = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = 30
        };
        foreach (var(name, value)in values)
        {
            if (name == "@cover")
                command.Parameters.Add(name, SqlDbType.VarBinary, -1).Value = value ?? DBNull.Value;
            else
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        using var reader = await command.ExecuteReaderAsync();
        var results = new List<List<SqlRow>>();
        do
        {
            var rows = new List<SqlRow>();
            while (await reader.ReadAsync())
            {
                var row = new SqlRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }

            if (reader.FieldCount > 0)
                results.Add(rows);
        }
        while (await reader.NextResultAsync());
        return results;
    }

    public async Task<List<SqlRow>> QueryAsync(string sql, params (string Name, object? Value)[] values) => (await BatchAsync(sql, values)).FirstOrDefault() ?? [];
    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] values)
    {
        using var command = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = 30
        };
        foreach (var(name, value)in values)
        {
            if (name == "@cover")
                command.Parameters.Add(name, SqlDbType.VarBinary, -1).Value = value ?? DBNull.Value;
            else
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<List<SqlRow>> ProcedureAsync(string name, params (string Name, object? Value)[] values)
    {
        using var command = new SqlCommand(name, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };
        foreach (var(key, value)in values)
        {
            if (value is DataTable table)
                command.Parameters.Add(new SqlParameter(key, SqlDbType.Structured) { TypeName = "dbo.DanhSachTepNoiDung", Value = table });
            else
                command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        }

        using var reader = await command.ExecuteReaderAsync();
        var rows = new List<SqlRow>();
        // Drain every result set before returning: usp_DocChuong commits after its second SELECT.
        do
        {
            while (await reader.ReadAsync())
            {
                var row = new SqlRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
        }
        while (await reader.NextResultAsync());
        return rows;
    }
}

public sealed class SqlDatabase(IConfiguration configuration)
{
    public async Task<T> ReadAsync<T>(Func<SqlSession, Task<T>> action)
    {
        using var connection = new SqlConnection(configuration.GetConnectionString("SachMoiDaDen") ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SachMoiDaDen."));
        await connection.OpenAsync();
        return await action(new SqlSession(connection));
    }

    public async Task<T> TransactionAsync<T>(Func<SqlSession, Task<T>> action)
    {
        using var connection = new SqlConnection(configuration.GetConnectionString("SachMoiDaDen"));
        await connection.OpenAsync();
        using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var result = await action(new SqlSession(connection, transaction));
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            if (transaction.Connection != null)
                await transaction.RollbackAsync();
            throw;
        }
    }
}
