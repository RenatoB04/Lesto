namespace OrderService.Services;

public interface IIdentityServiceClient
{
    Task<bool> IsCourierAsync(Guid userId, string token);
}