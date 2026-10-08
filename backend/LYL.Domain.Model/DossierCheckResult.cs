namespace LYL.Domain.Model;

public record DossierCheckResult(bool IsValid, IReadOnlyList<string> InvalidFields);