namespace ProductCare.Application.ProductManuals;

public sealed record ProductManualStatus(
    ProductManualState State,
    bool CanRetry,
    DateTime? RetryAfterUtc);