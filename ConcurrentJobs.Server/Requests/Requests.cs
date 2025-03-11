using System.ComponentModel.DataAnnotations;

namespace ConcurrentJobs.Server.Requests
{
    public record StartJobRequest([Required(ErrorMessage = "Job type is required")]
                             [StringLength(100, MinimumLength = 1, ErrorMessage = "Job type must be between 1 and 100 characters")]
                             string JobType,

                             [Required(ErrorMessage = "Job name is required")]
                             [StringLength(200, MinimumLength = 1, ErrorMessage = "Job name must be between 1 and 200 characters")]
                             string JobName);

    public record CancelJobRequest([Required(ErrorMessage = "Job ID is required")] Guid JobId);
}
