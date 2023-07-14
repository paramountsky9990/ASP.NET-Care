using HGP.Common.Database;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Services
{
    public interface IMatchedAssetService : IBaseService
    {
        void Save(MatchedAsset entry);
        MatchedAsset GetById(string id);

        MatchedAsset GetByWishListIDAndAssetID(string wishListID, string assetID);
        string Add(string wishListID, string assetID);
        bool UpdateEmailSent(string matchedAssetID);
        List<MatchedAsset> GetAllByWishListID(string wishListID);
    }

    public class MatchedAssetService : BaseService<MatchedAsset>, IMatchedAssetService
    {
        public MatchedAssetService(IMongoRepository repository, IWorkContext workContext)
            : base(repository, workContext)
        {
        }

        public string Add(string wishListID, string assetID)
        {
            string res = string.Empty;
            try
            {
                MatchedAsset matchedAsset = new MatchedAsset()
                {
                    WishLIstID = wishListID,
                    AssetID = assetID,
                    IsEmailSent = false,
                    Status=MatchedAssetStatusTypes.Matched
                };
                this.Save(matchedAsset);

                res = matchedAsset.Id;
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public List<MatchedAsset> GetAllByWishListID(string wishListID)
        {
            List<MatchedAsset> allMatchedAssets = new List<MatchedAsset>();
            try
            {
                allMatchedAssets = this.Repository.All<MatchedAsset>().Where(m => m.WishLIstID == wishListID).ToList();
            }
            catch (Exception ex) { throw; }
            return allMatchedAssets;
        }

        public MatchedAsset GetByWishListIDAndAssetID(string wishListID, string assetID)
        {
            MatchedAsset matchedAsset = null;
            try
            {
                matchedAsset = this.Repository.All<MatchedAsset>().Where(m =>
                    m.WishLIstID == wishListID && m.AssetID == assetID).FirstOrDefault();
            }
            catch (Exception ex) { throw; }
            return matchedAsset;
        }

        public bool UpdateEmailSent(string matchedAssetID)
        {
            bool res = false;
            try
            {
                MatchedAsset matchedAsset = this.GetById(matchedAssetID);
                matchedAsset.IsEmailSent = true;
                this.Save(matchedAsset);

                res = true;
            }
            catch (Exception ex) { throw; }
            return res;
        }
    }
}