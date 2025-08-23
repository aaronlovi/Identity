using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Statements;

internal sealed class UpdateUserRolesStmt : PostgresNonQueryBatchedDbStmtBase {
    private const string AddRoleSql = """
INSERT INTO user_roles (user_id, role, assigned_at)
VALUES (@user_id, @role, now_utc())
ON CONFLICT (user_id, role) DO NOTHING
""";

    private const string RemoveRoleSql = """
DELETE FROM user_roles
WHERE user_id = @user_id AND role = @role
""";

    internal UpdateUserRolesStmt(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove)
        : base(nameof(UpdateUserRolesStmt)) {
        foreach (string add in rolesToAdd) {
            AddCommandToBatch(AddRoleSql, [
                new NpgsqlParameter<long>("user_id", userId),
                new NpgsqlParameter<string>("role", add)
            ]);
        }

        foreach (string remove in rolesToRemove) {
            AddCommandToBatch(RemoveRoleSql, [
                new NpgsqlParameter<long>("user_id", userId),
                new NpgsqlParameter<string>("role", remove)
            ]);
        }
    }
}
