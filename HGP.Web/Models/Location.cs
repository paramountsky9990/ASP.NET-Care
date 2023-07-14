using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using HGP.Web.Database;

namespace HGP.Web.Models
{
    public interface ILocation
    {
        string Name { get; set; }
        Address Address { get; set; }
        string OwnerId { get; set; }
        string OwnerName { get; set; }
        string OwnerPhone { get; set; }
        string OwnerEmail { get; set; }
    }

    public class Location : ILocation
    {
        public string Name { get; set; }
        public Address Address { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }

        public Location()
        {
            this.Address = new Address();
        }
    }
}