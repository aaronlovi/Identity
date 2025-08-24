using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Identity.Infrastructure.Persistence.DTOs;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Statements;

internal sealed class GetUserStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = """
SELECT u.user_id, u.firebase_uid, string_agg(ur.role, ',') AS roles,
us.status,
u.created_at AS user_created_at, u.updated_at AS user_updated_at
FROM ${schema}.users u LEFT JOIN ${schema}.user_roles ur ON u.user_id = ur.user_id
LEFT JOIN ${schema}.user_status us ON u.user_id = us.user_id
WHERE u.user_id = @user_id
GROUP BY u.user_id, u.firebase_uid, us.status, u.created_at, u.updated_at
""";

    // Static SQL that gets set once on first constructor call
    private static string? _sql;
    private static readonly object _lock = new();

    private static int _userIdIndex = -1;
    private static int _firebaseUidIndex = -1;
    private static int _rolesIndex = -1;
    private static int _statusIndex = -1;
    private static int _createdAtIndex = -1;
    private static int _updatedAtIndex = -1;

    private readonly long _userId;

    public UserDTO User { get; private set; }

    public GetUserStmt(string schemaName, long userId)
        : base(GetSql(schemaName), nameof(GetUserStmt)) {
        _userId = userId;
        User = UserDTO.Empty;
    }

    protected override void BeforeRowProcessing(DbDataReader reader) {
        base.BeforeRowProcessing(reader);

        if (_userIdIndex != -1)
            return;

        _userIdIndex = reader.GetOrdinal("user_id");
        _firebaseUidIndex = reader.GetOrdinal("firebase_uid");
        _rolesIndex = reader.GetOrdinal("roles");
        _statusIndex = reader.GetOrdinal("status");
        _createdAtIndex = reader.GetOrdinal("user_created_at");
        _updatedAtIndex = reader.GetOrdinal("user_updated_at");
    }

    protected override void ClearResults() => User = UserDTO.Empty;

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [new NpgsqlParameter<long>("user_id", _userId)];

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        string roles = reader.GetString(_rolesIndex);
        List<string> rolesList = [.. roles.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)];

        User = new UserDTO(
            UserId: reader.GetInt64(_userIdIndex),
            FirebaseUid: reader.GetString(_firebaseUidIndex),
            Roles: rolesList,
            Status: reader.GetString(_statusIndex),
            CreatedAt: reader.GetDateTime(_createdAtIndex),
            UpdatedAt: reader.GetDateTime(_updatedAtIndex)
        );

        // Only one row is expected, so return false to stop further processing
        return false;
    }

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (_lock) {
                _sql = SqlTemplate.Replace("${schema}", schemaName);
            }
        }
        return _sql;
    }
}
