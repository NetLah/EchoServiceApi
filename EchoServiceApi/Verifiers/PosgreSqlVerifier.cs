#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped
namespace EchoServiceApi.Verifiers
{
    public class PosgreSqlVerifier : BaseVerifier
    {
        public PosgreSqlVerifier(IConfiguration configuration) : base(configuration) { }

        public async Task<VerifyResult> VerifyAsync(string name, string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            var connectionObj = GetConnection(name);
            var connectionString = connectionObj.Value;
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            var query = command.CommandText = "SELECT 1 FROM " + tableName;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();

            return VerifyResult.Successed("PosgreSql", connectionObj, detail: query);
        }
    }
}
#pragma warning restore S4457 // Parameter validation in "async"/"await" methods should be wrapped
