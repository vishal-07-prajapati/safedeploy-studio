namespace SafeDeployStudio.Domain.Enums;

public enum DeploymentStatus
{
    Draft = 1,
    PendingValidation = 2,
    ValidationFailed = 3,
    DryRunPassed = 4,
    DryRunFailed = 5,
    Deploying = 6,
    Succeeded = 7,
    Failed = 8,
    RolledBack = 9
}
