namespace CommonLibrary
{
    public class HealthResponse
    {
        public string Status { get; set; } = "healthy";
        public HashSet<ApplicationStatus> StatusList { get; set; }

        public HealthResponse()
        {
            StatusList = new HashSet<ApplicationStatus>();
        }
    }

    public class ApplicationStatus
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";

    }
}
