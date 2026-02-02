namespace FinanceManager.Infra.HostedServices
{
    public sealed class CleanupHostedService(IServiceProvider serviceProvider) : IHostedService, IDisposable
    {
        private Timer? _timer;

        public Task StartAsync(CancellationToken ct)
        {
            _timer = new Timer(_ => DoWork(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();

        private void DoWork()
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IdentityUserService>();
            
        }
    }
}
