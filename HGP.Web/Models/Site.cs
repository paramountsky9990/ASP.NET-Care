using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Requests;
using HGP.Web.Services;

namespace HGP.Web.Models
{
    public interface ISite : IMongoObjectBase
    {
        SiteSettings SiteSettings { get; set; }
        IList<Location> Locations { get; set; }
        IList<Category> Categories { get; set; }
        IList<ManufacturerSummary> Manufacturers { get; set; }
    }

    public class Site : MongoObjectBase, ISite
    {
        public SiteSettings SiteSettings { get; set; }
        public IList<Location> Locations { get; set; }
        public IList<Category> Categories { get; set; }
        public IList<ManufacturerSummary> Manufacturers { get; set; }
        public ContactInfo AccountExecutive { get; set; }

        public Site()
        {
            this.SiteSettings = new SiteSettings();
            this.Locations = new List<Location>();
            this.Manufacturers = new List<ManufacturerSummary>();
            this.Categories = new List<Category>();
            this.AccountExecutive = new ContactInfo();
        }

    }
}