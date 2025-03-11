using ConcurrentJobs.Server.Requests;
using ConcurrentJobs.Server.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;

namespace ConcurrentJobs.Server.TestsIntegration
{
    [TestFixture]
    public class JobControllerIntegrationTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task StartJob_ValidRequest_ReturnsOkWithJobId()
        {
            var request = new StartJobRequest("TestType", "Test Job");

            var response = await _client.PostAsJsonAsync("/api/jobs", request);
            var result = await response.Content.ReadFromJsonAsync<StartJobResponse>();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result!.JobId, Is.Not.EqualTo(Guid.Empty));
            });
        }

        [Test]
        public async Task StartJob_ExceedConcurrentLimit_ReturnsBadRequest()
        {
            // Start maximum number of jobs of the same type
            var jobType = $"ConcurrentTest-{Guid.NewGuid()}"; // Unique job type for this test

            for (int i = 0; i < 5; i++)
            {
                var request = new StartJobRequest(jobType, $"Concurrent Job {i}");
                var response = await _client.PostAsJsonAsync("/api/jobs", request);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }

            // Try to start one more
            var exceedingRequest = new StartJobRequest(jobType, "One Too Many");
            var exceedingResponse = await _client.PostAsJsonAsync("/api/jobs", exceedingRequest);

            Assert.That(exceedingResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            var errorMessage = await exceedingResponse.Content.ReadAsStringAsync();
            Assert.That(errorMessage, Does.Contain("Maximum number of concurrent jobs"));
        }

        [Test]
        public async Task GetJobStatus_ValidJobId_ReturnsCorrectStatus()
        {
            var request = new StartJobRequest("StatusTestType", "Status Test Job");
            var createResponse = await _client.PostAsJsonAsync("/api/jobs", request);
            var createResult = await createResponse.Content.ReadFromJsonAsync<StartJobResponse>();

            var statusResponse = await _client.GetAsync($"/api/jobs/{createResult!.JobId}");
            var statusResult = await statusResponse.Content.ReadFromJsonAsync<JobStatusResponse>();

            Assert.Multiple(() =>
            {
                Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(statusResult!.JobId, Is.EqualTo(createResult.JobId));
                Assert.That(statusResult.Status, Is.EqualTo("Running"));
                Assert.That(statusResult.JobType, Is.EqualTo("StatusTestType"));
                Assert.That(statusResult.JobName, Is.EqualTo("Status Test Job"));
            });
        }

        [Test]
        public async Task GetJobStatus_InvalidJobId_ReturnsNotFound()
        {
            var nonExistentJobId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/jobs/{nonExistentJobId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task CancelJob_ValidRunningJob_ReturnsOk()
        {
            var request = new StartJobRequest("CancelTestType", "Cancel Test Job");
            var createResponse = await _client.PostAsJsonAsync("/api/jobs", request);
            var createResult = await createResponse.Content.ReadFromJsonAsync<StartJobResponse>();

            var cancelRequest = new CancelJobRequest(createResult!.JobId);
            var cancelResponse = await _client.PostAsJsonAsync("/api/jobs/cancel", cancelRequest);

            Assert.That(cancelResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var statusResponse = await _client.GetAsync($"/api/jobs/{createResult.JobId}");
            var statusResult = await statusResponse.Content.ReadFromJsonAsync<JobStatusResponse>();
            Assert.That(statusResult!.Status, Is.EqualTo("Cancelled"));
        }

        [Test]
        public async Task CancelJob_InvalidJobId_ReturnsNotFound()
        {
            var nonExistentJobId = Guid.NewGuid();
            var cancelRequest = new CancelJobRequest(nonExistentJobId);

            var response = await _client.PostAsJsonAsync("/api/jobs/cancel", cancelRequest);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task EndToEndTest_JobCompletesAfterDelay()
        {
            var request = new StartJobRequest("CompletionTestType", "Completion Test Job");
            var createResponse = await _client.PostAsJsonAsync("/api/jobs", request);
            var createResult = await createResponse.Content.ReadFromJsonAsync<StartJobResponse>();

            var jobId = createResult!.JobId;
            JobStatusResponse? statusResult = null;

            // Poll until job completes or times out
            var timeout = DateTime.UtcNow.AddSeconds(20); // More than the 15 seconds job delay
            while (DateTime.UtcNow < timeout)
            {
                var statusResponse = await _client.GetAsync($"/api/jobs/{jobId}");
                statusResult = await statusResponse.Content.ReadFromJsonAsync<JobStatusResponse>();

                if (statusResult!.Status != "Running")
                    break;

                await Task.Delay(500); // Poll every 500ms
            }

            Assert.That(statusResult, Is.Not.Null);
            Assert.That(statusResult!.Status, Is.EqualTo("Completed"));
        }
    }
}
