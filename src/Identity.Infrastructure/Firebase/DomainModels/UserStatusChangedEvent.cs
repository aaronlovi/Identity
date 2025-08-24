using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Firebase.DomainModels;

/// <summary>
/// Event data for UserStatusChanged events.
/// </summary>
public record UserStatusChangedEvent(
    long UserId,
    string PreviousStatus,
    string NewStatus,
    string ChangedBy,
    DateTime ChangedAt);
