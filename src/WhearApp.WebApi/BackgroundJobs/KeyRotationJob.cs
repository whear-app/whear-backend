using Quartz;
using WhearApp.Infrastructure.Identity.Security;

namespace WhearApp.WebApi.BackgroundJobs;

[DisallowConcurrentExecution]
public class KeyRotationJob(IKeyManagementService keyService, ILogger<KeyRotationJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Executing scheduled key rotation...");
            await keyService.RotateKey();
            logger.LogInformation("Scheduled key rotation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rotate keys");
            throw;
        }
    }
}