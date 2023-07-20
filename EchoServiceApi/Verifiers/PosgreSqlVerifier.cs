namespace EchoServiceApi.Verifiers;

public class PosgreSqlVerifier : BaseVerifier
{
    public PosgreSqlVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task<VerifyResult> VerifyAsync(string name, string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            throw new ArgumentNullException(nameof(tableName));
        }

        var connectionObj = GetConnection(name);
        var connectionString = connectionObj.Value;
        using var connection = new Npgsql.NpgsqlConnection(connectionString);

        Logger.LogInformation("PosgreSqlVerifier: name={query_name} tableName={query_tableName}", name, tableName);

        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        var query = command.CommandText = "SELECT 1 FROM " + tableName;
        command.CommandType = System.Data.CommandType.Text;
        command.ExecuteNonQuery();

        return VerifyResult.Succeed("PosgreSql", connectionObj, detail: query);
    }
}