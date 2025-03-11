using ConcurrentJobs.Server.Core.Jobs;
using ConcurrentJobs.Server.Core.Services;
using System.Collections.Concurrent;

namespace ConcurrentJobs.Server.Services
{
    public class JobService : IJobService
    {
        private readonly ConcurrentDictionary<Guid, Job> _jobs = new();
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellationTokens = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _jobTypeLimits = new();
        private const int MaxConcurrentJobsPerType = 5;

        public async Task<Guid> StartJobAsync(string jobType, string jobName)
        {
            // Get or create semaphore for this job type
            var semaphore = _jobTypeLimits.GetOrAdd(jobType, _ => new SemaphoreSlim(MaxConcurrentJobsPerType, MaxConcurrentJobsPerType));

            // Try to acquire semaphore
            if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                throw new InvalidOperationException($"Cannot start job. Maximum number of concurrent jobs ({MaxConcurrentJobsPerType}) for type '{jobType}' has been reached.");
            }

            try
            {
                var jobId = Guid.NewGuid();
                var job = new Job
                {
                    Id = jobId,
                    JobType = jobType,
                    JobName = jobName,
                    Status = JobStatus.Running,
                    CreatedAt = DateTime.UtcNow
                };

                _jobs[jobId] = job;

                // Create cancellation token
                var cts = new CancellationTokenSource();
                _jobCancellationTokens[jobId] = cts;

                // Start the job (fire and forget)
                _ = ProcessJobAsync(jobId, semaphore, cts.Token);

                return jobId;
            }
            catch
            {
                // Release semaphore if job setup fails
                semaphore.Release();
                throw;
            }
        }

        public Task<Job?> GetJobAsync(Guid jobId)
        {
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<bool> CancelJobAsync(Guid jobId)
        {
            if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
            {
                // Request cancellation
                cts.Cancel();

                // Update job status
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    job.Status = JobStatus.Cancelled;
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private async Task ProcessJobAsync(Guid jobId, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            try
            {
                // Simulate long-running work with a delay
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

                if (_jobs.TryGetValue(jobId, out var job))
                {
                    job.Status = JobStatus.Completed;
                    job.CompletedAt = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                // Job was cancelled - status already updated in CancelJobAsync
            }
            catch (Exception)
            {
                // Update job status to failed
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    job.Status = JobStatus.Failed;
                    job.CompletedAt = DateTime.UtcNow;
                }
            }
            finally
            {
                // Clean up resources
                if (_jobCancellationTokens.TryRemove(jobId, out var cts))
                {
                    cts.Dispose();
                }

                // Release semaphore to allow another job of this type to start
                semaphore.Release();
            }
        }
    }
}
