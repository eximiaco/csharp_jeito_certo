using System.Collections.Immutable;

namespace Eximia.Integrations.Jobs;

public record JobsHealth
{
    public JobsHealth(IEnumerable<JobInfo> jobs)
    {
        Jobs = jobs.ToImmutableArray();
    }

    public ImmutableArray<JobInfo> Jobs { get; }
    public bool IsHealthy => Jobs.All(c => c.IsHealthy);

    public void SetJobHealth(string job, bool isHealthy)
    {
        var jobInfo = Jobs.FirstOrDefault(j => j.Name == job);
        if (jobInfo is null)
            return;

        jobInfo.IsHealthy = isHealthy;
    }

    public record JobInfo
    {
        private int _isHealthy;

        public JobInfo(string name, bool isHealthy)
        {
            Name = name;
            _isHealthy = isHealthy ? 1 : 0;
        }

        public string Name { get; }
        public bool IsHealthy
        {
            get
            {
                // Leitura atômica do valor com garantia de visibilidade entre threads
                return Interlocked.CompareExchange(ref _isHealthy, 0, 0) == 1;
            }
            set
            {
                // Escrita atômica do valor
                Interlocked.Exchange(ref _isHealthy, value ? 1 : 0);
            }
        }

        public static JobInfo Create(string job)
            => new JobInfo(job, isHealthy: true);
    }
}
