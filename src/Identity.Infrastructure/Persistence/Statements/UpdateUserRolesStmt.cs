using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Statements;

internal sealed class UpdateUserRolesStmt : PostgresNonQueryBatchedDbStmtBase {
    private const string AddRoleSqlTemplate = """
INSERT INTO ${schema}.user_roles (user_id, role, assigned_at)
VALUES (@user_id, @role, now_utc())
ON CONFLICT (user_id, role) DO NOTHING
""";

    private const string RemoveRoleSqlTemplate = """
DELETE FROM ${schema}.user_roles
WHERE user_id = @user_id AND role = @role
""";

    // Static SQL that gets set once on first constructor call
    private static string? _addRoleSql;
    private static string? _removeRoleSql;
    private static readonly object _lock = new();

    internal UpdateUserRolesStmt(string schemaName, long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove)
        : base(nameof(UpdateUserRolesStmt)) {
        
        (string addSql, string removeSql) = GetSql(schemaName);
        
        foreach (string add in rolesToAdd) {
            AddCommandToBatch(addSql, [
                new NpgsqlParameter<long>("user_id", userId),
                new NpgsqlParameter<string>("role", add)
            ]);
        }

        foreach (string remove in rolesToRemove) {
            AddCommandToBatch(removeSql, [
                new NpgsqlParameter<long>("user_id", userId),
                new NpgsqlParameter<string>("role", remove)
            ]);
        }
    }

    private static (string addSql, string removeSql) GetSql(string schemaName) {
        if (_addRoleSql is null || _removeRoleSql is null) {
            lock (_lock) {
                _addRoleSql ??= AddRoleSqlTemplate.Replace("${schema}", schemaName);
                _removeRoleSql ??= RemoveRoleSqlTemplate.Replace("${schema}", schemaName);
            }
        }
        return (_addRoleSql, _removeRoleSql);
    }
}
