using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Statements;

internal sealed class SetUserStatusStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = """
UPDATE ${schema}.user_status
SET status = @status, updated_at = now_utc()
WHERE user_id = @user_id
""";

    // Static SQL that gets set once on first constructor call
    private static string? _sql;
    private static readonly object _lock = new();

    private readonly long _userId;
    private readonly string _status;

    public SetUserStatusStmt(string schemaName, long userId, string status)
        : base(GetSql(schemaName), nameof(SetUserStatusStmt)) {
        _userId = userId;
        _status = status;
    }

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (_lock) {
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
            }
        }
        return _sql;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [new NpgsqlParameter<long>("user_id", _userId),
         new NpgsqlParameter<string>("status", _status)];
}
