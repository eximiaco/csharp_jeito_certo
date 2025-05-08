using System;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceManager.Crosscutting.Healthchecks
{
    public class ResourceCheckerHealthCkeck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var memoryUsage = GC.GetTotalMemory(false) /  1_000_000;
            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);

            return Task.FromResult(HealthCheckResult.Healthy(data: new Dictionary<string, object>
            {
                { "MemoryUsage (MB)", memoryUsage },
                { "MinWorkerThreads", workerThreads },
                { "MaxWorkerThreads", maxWorkerThreads },
                { "MinCompletionPortThreads", completionPortThreads },
                { "MaxCompletionPortThreads", maxCompletionPortThreads },
                { "AvailableWorkerThreads", availableWorkerThreads },
                { "AvailableCompletionPortThreads", availableCompletionPortThreads }
            }));
        }
    }
}
