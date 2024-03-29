namespace HealthAPI
{
    public class HealthResponse
    {
        public string status { get; set; } = "healthy";
        public List<ApplicationStatus> statusList { get; set; }

        public HealthResponse()
        {
            statusList = new List<ApplicationStatus>();
        }
    }

    public class ApplicationStatus
    {
        public string Name { get; set; }
        public string Status { get; set; }

    }
}
