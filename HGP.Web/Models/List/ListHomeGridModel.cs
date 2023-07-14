using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Models.Admin;

namespace HGP.Web.Models.List
{
    public class ListHomeGridModel
    {
        public string Id { get; set; }
        public int Sequence { get; set; }
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public int Quantity { get; set; }
        public string BookValue { get; set; }
        public bool DisplayBookValue { get; set; }
        public string Location { get; set; }
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public DateTime AvailForSale { get; set; }
        public Double MinutesRemaining { get; set; }
        public IList<AdminAssetsHomeGridMediaModel> Media { get; set; }
    }
}