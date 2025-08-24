using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Firebase.DomainModels;

/// <summary>
/// Event data for UserRolesUpdated events.
/// </summary>
public record UserRolesUpdatedEvent(
    long UserId,
    List<string> AddedRoles,
    List<string> RemovedRoles,
    List<string> Roles,
    string ChangedBy,
    DateTime ChangedAt);
