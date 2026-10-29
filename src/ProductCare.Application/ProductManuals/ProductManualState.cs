namespace ProductCare.Application.ProductManuals;

public enum ProductManualState
{
    NotUploaded,
    PendingSecurityScan,
    SecurityRejected,
    SecurityScanFailed,
    PendingIndexing,
    Ready,
    Failed
}
