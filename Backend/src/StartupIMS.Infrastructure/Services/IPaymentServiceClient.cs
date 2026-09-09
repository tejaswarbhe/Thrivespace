using StartupIMS.Shared.DTOs;

namespace StartupIMS.Infrastructure.Services;

public interface IPaymentServiceClient
{
    Task<InitiatePaymentResponse> InitiatePaymentAsync(int fundingRequestId, decimal amount, string currency);
}
