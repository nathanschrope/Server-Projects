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

    public class ApplicationStatusComparer : IEqualityComparer<ApplicationStatus>
    {
        public bool Equals(ApplicationStatus? x, ApplicationStatus? y)
        {
            if (x == null || y == null)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase) && x.Status.Equals(y.Status, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ApplicationStatus? obj)
        {
            if (obj == null)
                return 0;

            return obj.Name.GetHashCode() ^ obj.Status.GetHashCode();  
        }
    }
}
