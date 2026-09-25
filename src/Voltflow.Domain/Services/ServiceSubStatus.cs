namespace Voltflow.Domain.Services;

public enum ServiceSubStatus
{
    None = 0,
    
    // For Draft / Issued
    WaitingForInternalApproval = 10,
    AwaitingCustomerResponse = 11,
    InRevision = 12,

    // For Active
    WaitingForMaterials = 20,
    Scheduled = 21,
    InTransit = 22,
    OnSite = 23,
    WorkInProgress = 24,
    QualityCheck = 25,
    PendingCustomerApproval = 26,

    // For OnHold
    WaitingForAccess = 30,
    WaitingForPayment = 31,
    WeatherDelay = 32,
    
    // For Completed
    Invoiced = 40,
    Paid = 41
}
