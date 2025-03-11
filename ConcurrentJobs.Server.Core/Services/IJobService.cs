using ConcurrentJobs.Server.Core.Jobs;

namespace ConcurrentJobs.Server.Core.Services
{
    public interface IJobService
    {
        /// <summary>
        /// Starts a new job of the specified type and name.
        /// </summary>
        /// <param name="jobType">Job type</param>
        /// <param name="jobName">Job name</param>
        /// <returns></returns>
        Task<Guid> StartJobAsync(string jobType, string jobName);

        /// <summary>
        /// Gets the job with the specified ID.
        /// </summary>
        /// <param name="jobId">Job id to retrieve</param>
        /// <returns></returns>
        Task<Job?> GetJobAsync(Guid jobId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId">Job id to cancel</param>
        /// <returns></returns>
        Task<bool> CancelJobAsync(Guid jobId);
    }
}
