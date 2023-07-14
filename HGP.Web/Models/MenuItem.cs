namespace HGP.Web.Models
{
    public class MenuItem
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public string LinkText { get; set; }
        public bool IsActive { get; set; }
        public string Tag { get; set; }
    }
}