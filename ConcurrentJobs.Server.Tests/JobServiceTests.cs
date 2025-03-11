using ConcurrentJobs.Server.Core.Jobs;
using ConcurrentJobs.Server.Services;

namespace ConcurrentJobs.Server.Tests
{
    public class JobServiceTests
    {
        private JobService _jobService;

        [SetUp]
        public void Setup()
        {
            _jobService = new JobService();
        }

        [Test]
        public async Task StartJobAsync_ReturnsValidJobId()
        {
            var jobId = await _jobService.StartJobAsync("TestType", "Test Job");

            Assert.That(jobId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public async Task GetJobAsync_AfterStarting_ReturnsRunningJob()
        {
            var jobId = await _jobService.StartJobAsync("TestType", "Test Job");

            var job = await _jobService.GetJobAsync(jobId);

            Assert.That(job, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(job.Id, Is.EqualTo(jobId));
                Assert.That(job.JobType, Is.EqualTo("TestType"));
                Assert.That(job.JobName, Is.EqualTo("Test Job"));
                Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            });
        }

        [Test]
        public async Task GetJobAsync_WithNonExistentId_ReturnsNull()
        {
            var job = await _jobService.GetJobAsync(Guid.NewGuid());

            Assert.That(job, Is.Null);
        }

        [Test]
        public async Task CancelJobAsync_OnRunningJob_ReturnsTrue()
        {
            var jobId = await _jobService.StartJobAsync("TestType", "Test Job");

            var result = await _jobService.CancelJobAsync(jobId);
            var job = await _jobService.GetJobAsync(jobId);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(job, Is.Not.Null);
            });

            Assert.That(job.Status, Is.EqualTo(JobStatus.Cancelled));
        }

        [Test]
        public async Task CancelJobAsync_OnNonExistentJob_ReturnsFalse()
        {
            var result = await _jobService.CancelJobAsync(Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task Job_CompletesSuccessfully_AfterDelay()
        {
            var jobId = await _jobService.StartJobAsync("TestType", "Test Job");

            // Wait for job to complete (slightly longer than the 15-second job duration)
            await Task.Delay(TimeSpan.FromSeconds(16));
            var job = await _jobService.GetJobAsync(jobId);

            Assert.That(job, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(job.Status, Is.EqualTo(JobStatus.Completed));
                Assert.That(job.CompletedAt, Is.Not.Null);
            });
        }

        [Test]
        public async Task StartJobAsync_ThrowsException_WhenMaxConcurrentJobsReached()
        {
            const string jobType = "TestType";
            const int maxJobs = 5;

            var jobIds = new List<Guid>();
            for (int i = 0; i < maxJobs; i++)
            {
                var jobId = await _jobService.StartJobAsync(jobType, $"Test Job {i}");
                jobIds.Add(jobId);
            }

            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _jobService.StartJobAsync(jobType, "One Too Many"));

            Assert.That(exception.Message, Contains.Substring("Maximum number of concurrent jobs"));
        }

        [Test]
        public async Task StartJobAsync_AllowsNewJob_AfterOneCompletes()
        {
            const string jobType = "TestType";
            const int maxJobs = 5;

            var jobIds = new List<Guid>();
            for (int i = 0; i < maxJobs; i++)
            {
                var jobId = await _jobService.StartJobAsync(jobType, $"Test Job {i}");
                jobIds.Add(jobId);
            }

            await _jobService.CancelJobAsync(jobIds.First());

            await Task.Delay(500);

            var newJobId = await _jobService.StartJobAsync(jobType, "New Job After Cancellation");

            var job = await _jobService.GetJobAsync(newJobId);
            Assert.That(job, Is.Not.Null);
            Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
        }

        [Test]
        public async Task StartJobAsync_AllowsMaxJobsOfDifferentTypes()
        {
            const int maxJobs = 5;

            for (int i = 0; i < maxJobs; i++)
            {
                await _jobService.StartJobAsync("TypeA", $"Job A{i}");
            }

            var jobIdsTypeB = new List<Guid>();
            for (int i = 0; i < maxJobs; i++)
            {
                var jobId = await _jobService.StartJobAsync("TypeB", $"Job B{i}");
                jobIdsTypeB.Add(jobId);
            }
            Assert.That(jobIdsTypeB, Has.Count.EqualTo(maxJobs));

            // Verify all jobs of second type are running
            foreach (var jobId in jobIdsTypeB)
            {
                var job = await _jobService.GetJobAsync(jobId);
                Assert.That(job, Is.Not.Null);
                Assert.That(job.Status, Is.EqualTo(JobStatus.Running));
            }
        }
    }
}
