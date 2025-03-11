using ConcurrentJobs.Server.Core.Services;
using ConcurrentJobs.Server.Requests;
using ConcurrentJobs.Server.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ConcurrentJobs.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController(IJobService jobService, ILogger<JobsController> logger) : ControllerBase
    {
        private readonly IJobService _jobService = jobService;
        private readonly ILogger<JobsController> _logger = logger;

        [HttpPost]
        [ProducesResponseType(typeof(StartJobResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartJob([FromBody] StartJobRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Invalid job request: {@ValidationErrors}", errors);
                return BadRequest(new { Errors = errors });
            }

            try
            {
                var jobId = await _jobService.StartJobAsync(request.JobType, request.JobName);
                _logger.LogInformation("Job started: {JobId} ({JobType}: {JobName})", jobId, request.JobType, request.JobName);
                return Ok(new StartJobResponse(jobId));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to start job {@JobDetails}", new { request.JobType, request.JobName, ErrorMessage = ex.Message });
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting job {@JobDetails}", new { request.JobType, request.JobName, ErrorType = ex.GetType().Name });
                return StatusCode(500, "An error occurred while starting the job.");
            }
        }

        [HttpGet("{jobId:guid}")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            try
            {
                var job = await _jobService.GetJobAsync(jobId);

                if (job == null)
                {
                    _logger.LogWarning("Job status request for non-existent job: {JobId}", jobId);
                    return NotFound($"Job with ID {jobId} not found.");
                }

                var statusResponse = new JobStatusResponse(
                    job.Id,
                    job.JobType,
                    job.JobName,
                    job.Status.ToString()
                );

                return Ok(statusResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
                return StatusCode(500, "An error occurred while retrieving the job status.");
            }
        }

        [HttpPost("cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelJob([FromBody] CancelJobRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Invalid cancel request: {@ValidationErrors}", errors);
                return BadRequest(new { Errors = errors });
            }

            try
            {
                var cancelled = await _jobService.CancelJobAsync(request.JobId);

                if (!cancelled)
                {
                    _logger.LogWarning("Failed to cancel job: {JobId} - Job not found or already completed", request.JobId);
                    return NotFound($"Job with ID {request.JobId} not found or already completed.");
                }

                _logger.LogInformation("Job cancelled: {JobId}", request.JobId);
                return Ok($"Job with ID {request.JobId} has been cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", request.JobId);
                return StatusCode(500, "An error occurred while attempting to cancel the job.");
            }
        }
    }
}
