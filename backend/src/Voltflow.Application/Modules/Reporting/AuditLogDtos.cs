using System;

namespace Voltflow.Application.Dtos;

public record AuditLogDto(Guid Id, Guid UserId, string Action, string EntityName, string EntityId, string Details, DateTime Timestamp);
public record CreateAuditLogRequest(Guid UserId, string Action, string EntityName, string EntityId, string Details);
