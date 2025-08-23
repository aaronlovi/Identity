using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Statements;

internal sealed class SetUserStatusStmt : PostgresNonQueryDbStmtBase {
    private const string SQL = """
UPDATE user_status
SET status = @status, updated_at = now_utc()
WHERE user_id = @user_id
""";

    private readonly long _userId;
    private readonly string _status;

    public SetUserStatusStmt(long userId, string status)
        : base(SQL, nameof(SetUserStatusStmt)) {
        _userId = userId;
        _status = status;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [new NpgsqlParameter<long>("user_id", _userId),
         new NpgsqlParameter<string>("status", _status)];
}
