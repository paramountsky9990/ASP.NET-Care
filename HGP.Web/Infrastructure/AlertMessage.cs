namespace HGP.Web.Infrastructure
{
    public enum AlertSeverity
    {
        Message = 1,
        Error = 2,
        Warning = 3,
        Success = 4
    }

    public class AlertMessage
    {
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
    }
}
