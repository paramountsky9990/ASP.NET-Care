using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Assets
{
    public class CategoryListModel
    {
        public string Name { get; set; }
        public string UriString { get; set; }
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string LinkText { get; set; }
        public string Tag { get; set; }
    }

    public class RecentCategory
    {
        public string PortalId { get; set; }
        public string Name { get; set; }
        public string UriString { get; set; }
        public int Count { get; set; }
    }

    public class Category
    {
        public string Name { get; set; }
        public string UriString { get; set; }
        public int Count { get; set; }
    }

        

    public class ManufacturerSummary
    {
        public string Name { get; set; }
        public string UriString { get; set; }
        public int Count { get; set; }
    }

    public class LocationListModel
    {
        public string Name { get; set; }
        public string UriString { get; set; }
        public int Count { get; set; }
        public bool IsVisible { get; set; }
        public bool IsActive { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string LinkText { get; set; }
        public string Tag { get; set; }
    }
}
