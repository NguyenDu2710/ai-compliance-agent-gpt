namespace AIComplianceAgent.Api.Contracts;

public sealed record WebhookValidationResult(bool IsAuthorized, string Payload, IReadOnlyDictionary<string, string> Headers);
