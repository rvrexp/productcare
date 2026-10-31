namespace ProductCare.Application.Accounts;

public interface IProductResourceCleanupService
{
    Task DeleteAsync(Guid productId, string? assistantConversationId, CancellationToken cancellationToken = default);
}
