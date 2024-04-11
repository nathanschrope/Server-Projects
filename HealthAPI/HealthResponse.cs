namespace HealthAPI
{
    public class HealthResponse
    {
        public string Status { get; set; } = "healthy";
        public List<ApplicationStatus> StatusList { get; set; }

        public HealthResponse()
        {
            StatusList = new List<ApplicationStatus>();
        }
    }

    public class ApplicationStatus
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";

    }
}
