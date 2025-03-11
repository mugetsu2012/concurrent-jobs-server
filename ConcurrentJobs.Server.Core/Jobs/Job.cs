namespace ConcurrentJobs.Server.Core.Jobs
{
    public class Job
    {
        public Guid Id { get; set; }

        public string JobType { get; set; } = string.Empty;

        public string JobName { get; set; } = string.Empty;

        public JobStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
