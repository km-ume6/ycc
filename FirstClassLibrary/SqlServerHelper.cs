using Microsoft.Data.SqlClient;
using System.Data;

namespace FirstClassLibrary
{
    /// <summary>
    /// SQL Serverへの汎用アクセスヘルパークラス。
    /// 接続管理、クエリ実行、パラメータ対応、リソース解放をサポートします。
    /// </summary>
    /// <example>
    /// <code>
    /// using var db = new SqlServerHelper("Server=...;Database=...;User Id=...;Password=...;");
    /// var dt = db.ExecuteQuery("SELECT * FROM Users WHERE Id = @id", new SqlParameter("@id", 1));
    /// int count = db.ExecuteNonQuery("UPDATE Users SET Name = @name WHERE Id = @id", new SqlParameter("@name", "新しい名前"), new SqlParameter("@id", 1));
    /// object? value = db.ExecuteScalar("SELECT COUNT(*) FROM Users");
    /// </code>
    /// </example>
    public class SqlServerHelper : IDisposable
    {
        private readonly SqlConnection _connection;
        static public string ConnectionString { get; set; } = string.Empty;

        public SqlServerHelper()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("ConnectionString is not set. Please set it before using SqlServerHelper.");

            // 修正: コンストラクタの呼び出しではなく、フィールドの初期化を行う
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

        /// <summary>
        /// 接続文字列でSQL Serverへ接続します。
        /// </summary>
        /// <param name="connectionString">SQL Serverの接続文字列</param>
        public SqlServerHelper(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        /// <summary>
        /// SELECT文などのクエリを実行し、結果をDataTableで取得します。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameters">SQLパラメータ（省略可）</param>
        /// <returns>クエリ結果のDataTable</returns>
        public DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, _connection);
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);

            return table;
        }

        /// <summary>
        /// INSERT/UPDATE/DELETEなどの非クエリSQLを実行します。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameters">SQLパラメータ（省略可）</param>
        /// <returns>影響を受けた行数</returns>
        public int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, _connection);
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 単一値（集計値など）を取得します。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameters">SQLパラメータ（省略可）</param>
        /// <returns>取得した値（object型、null可）</returns>
        public object? ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, _connection);
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// 接続を破棄し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}