using Quartz;
using WhearApp.WebApi.BackgroundJobs;

namespace WhearApp.WebApi.Extensions.DI;

public static class BackgroundJobExtensions
{
    public static void AddBackgroundJobServices(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("KeyRotationJob");
            q.AddJob<KeyRotationJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("KeyRotationTrigger")
                .WithCronSchedule("0 0 2 */30 * ?")); // 2 AM every 30 days
            // .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever())); // Test: mỗi 24 giờ
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }

}