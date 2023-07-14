using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using AutoMapper;
using HGP.Common;
using HGP.Common.Database;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Models;
using HGP.Web.Models.Email;
using Limilabs.Client.SMTP;
using Limilabs.Mail;
using Limilabs.Mail.Fluent;
using Limilabs.Mail.Headers;
using Limilabs.Mail.MIME;
using Limilabs.Mail.Templates;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.Provider;
using WebGrease.Css.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;

namespace HGP.Web.Services
{
    public class EmailSender<T> where T : EmailModel
    {
        private static ILogger Logger { get; set; }

        public EmailSender()
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("EmailSender");

            this.Repository = new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"],
                WebConfigurationManager.AppSettings["MongoDbName"]);
        }

        public IMongoRepository Repository { get; private set; }

        private EmailTemplate LoadTemplate(GlobalConstants.EmailTypes emailType, string portalId)
        {
            var templateFileName = Enum.GetName(typeof(GlobalConstants.EmailTypes), emailType);
            var template =
                this.Repository.All<EmailTemplate>()
                    .FirstOrDefault(x => x.TemplateType == templateFileName && x.PortalId == portalId);

            if (template == null)
            {
                // Load the default template if no portal specific template is found
                template = this.Repository.All<EmailTemplate>().FirstOrDefault(x => x.TemplateType == templateFileName);
                if (template == null)
                {
                    //Read from file system
                    var path = AppDomain.CurrentDomain.BaseDirectory;
                    var data = File.ReadAllText(path + @"/TestData/EmailTemplates/" + templateFileName + ".html");
                    template = new EmailTemplate()
                    {
                        Data = data,
                    };
                }
            }

            return template;
        }

        public async Task<EmailResult> SendEmail(GlobalConstants.EmailTypes emailType, T emailModel)
        {
            var result = new EmailResult() {SendStatus = SendMessageStatus.Failure};

            var header = LoadTemplate(GlobalConstants.EmailTypes.Header, emailModel.PortalId);
            var footer = LoadTemplate(GlobalConstants.EmailTypes.Footer, emailModel.PortalId);
            var body = LoadTemplate(emailType, emailModel.PortalId);

            try
            {
                Limilabs.Mail.Log.Enabled = bool.Parse(WebConfigurationManager.AppSettings["SMTPLogEnabled"]);

                if (Limilabs.Mail.Log.Enabled)
                {
                    Trace.Listeners.Add(new MyListener(WebConfigurationManager.AppSettings["SMTPLogPath"]));
                    Trace.AutoFlush = true;
                }

                Template emailerTemplate = Template.Create(header.Data + body.Data + footer.Data)
                    .DataFrom(emailModel);
                var emailRendered = emailerTemplate.Render();

                MailBuilder builder = new MailBuilder();
                builder.Html = emailRendered;
                builder.Subject = emailModel.Subject;
                builder.From.Add(new MailBox(WebConfigurationManager.AppSettings["FromAddress"],
                    WebConfigurationManager.AppSettings["FromName"]));

                var toAddresses = new MailAddressParser().Parse(emailModel.ToAddress);
                foreach (var mailAddress in toAddresses)
                {
                    builder.To.Add(mailAddress);
                }

                var ccAddresses = new MailAddressParser().Parse(emailModel.CcAddress);
                foreach (var mailAddress in ccAddresses)
                {
                    builder.Cc.Add(mailAddress);
                }

                IMail email = builder.Create();

                var sendEmail = bool.Parse(WebConfigurationManager.AppSettings["SMTPSendEnabled"]);
                if (sendEmail)
                    using (Smtp smtp = new Smtp())
                    {
                        smtp.Connect(emailModel.SMTPServer); // or ConnectSSL for SSL
                        smtp.UseBestLogin(emailModel.SMTPUserName, emailModel.SMTPPassword);


                        var sendResult = smtp.SendMessage(email);
                        result.SendStatus = (SendMessageStatus) sendResult.Status;

                        smtp.Close();
                    }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
                Console.WriteLine(ex);

                //throw; Do not rethrow
            }

            return result; //todo: Return real error code
        }

        private byte[] ReadImage(string filePath)
        {
            byte[] data;

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var size = (int) stream.Length; // Returns the length of the file
                data = new byte[size]; // Initializes and array in which to store the file
                stream.Read(data, 0, size);
                stream.Seek(0, SeekOrigin.Begin);

                var memStream = new MemoryStream();
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }
    }

    public interface IEmailService : IBaseService
    {
        void Save(EmailTemplate emailTemplate);
        void Delete(string portalId, GlobalConstants.EmailTypes templateType);
        void Delete(string emailTemplateId);
        void Exists(EmailTemplate emailTemplate);
        EmailTemplate GetById(string emailTemplateId);

        Task<EmailResult> SendOwnerNotificationPendingApproval(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendLocationNotificationApproved(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendLocationPendingApprovalNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendRequestorNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendRequestorNotificationToOthers(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendManagerPendingApproval(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendManagerPendingApproval(Site site, PortalUser requestor, Request request);
        Task<EmailResult> SendWelcomeMessage(PortalUser user, Site site, string callbackUrl);
        Task<EmailResult> SendWelcomeMessage4AdminUser(PortalUser user, Site site);
        Task<EmailResult> SendResetPasswordNotification(PortalUser user, Site site, string callbackUrl);

        Task<EmailResult> SendAssetUploadSummary(HttpContext cContext, Site site, PortalUser user,
            List<AssetsUploaded> assetsUploaded);

        Task<EmailResult> SendPendingRequestReminder(PendingRequestReminderModel pendingReqReminderModel);
        Task<EmailResult> SendRequestAssetNotAvailable(Site site, Request request, Asset asset);

        Task<EmailResult> SendWishListMatchedAssets(PortalUser user, List<AssetsUploaded> assetsUploaded,
            WishList wishList, HttpContext cContext, Site site);

        Task<EmailResult> SendSoonToBeExpiringWishLists(HttpContext cContext, Site site, PortalUser user,
            List<WishList> wishlists);

        Task<EmailResult> DoSendAssetApprovedNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> DoSendRequestApprovedToOthers(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> DoSendRequestDeniedNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail);

        Task<EmailResult> SendExpiringAssetsMessage(HttpContext cContext, Site site, PortalUser user,
            List<Asset> expiringAssets);

        Task<EmailResult> SendDraftAssetPendingApproval(Site site, DraftAsset draftAsset, HttpContext cContext);

        Task<EmailResult> SendDraftAssetDeniedApproval(Site site, DraftAsset draftAsset, string message, HttpContext cContext);

        void SendDraftAssetApprovedMessage(Site site, PortalUser assetOwner, DraftAsset draftAsset, HttpContext workContextHttpContext);
    }

    public class EmailServiceMappingProfile : Profile
    {
        public EmailServiceMappingProfile()
        {
            CreateMap<AssetRequestDetail, AssetRequestEmailDto>();
            CreateMap<Request, EmailTaskModel>();
            CreateMap<Request, AssetApprovedEmailModel>();
            CreateMap<Request, AssetDeniedEmailModel>();
            CreateMap<DraftAsset, DraftAssetDeniedEmailModel>();
            CreateMap<DraftAsset, EmailTaskModel>();
            
        }
    }

    public class EmailService : BaseService<EmailTemplate>, IEmailService
    {

        public void Delete(string portalId, GlobalConstants.EmailTypes templateType)
        {
            this.Repository.Delete<EmailTemplate>(
                x => x.PortalId == portalId && x.TemplateType == templateType.ToString());
        }

        public new void Delete(string emailTemplateId)
        {
            // Add business logic to remove a site here
            // todo: Delete associated objects (assets etc.)

            var template = this.GetById(emailTemplateId);
            this.Repository.Delete<EmailTemplate>(x => x.Id == template.Id);

            base.Delete(emailTemplateId);
        }

        public void Exists(EmailTemplate emailTemplate)
        {
            throw new System.NotImplementedException();
        }

        public async Task<EmailResult> SendOwnerNotificationPendingApproval(Site site, PortalUser requestor,
            Request request, AssetRequestDetail requestDetail)
        {
            var emailModel = new EmailTaskModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var approvingManager = userManager.FindById(request.ApprovingManagerId);

            Mapper.Map<Request, EmailTaskModel>(request, emailModel);
            emailModel.RequestId = requestor.Id;
            emailModel.AssetRequestDetail = requestDetailDto;
            emailModel.Request = request;

            emailModel.PrimaryImageUrl = string.Empty;
            if (requestDetailDto.Media != null & requestDetailDto.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + requestDetailDto.Media[0].FileName;
            }

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = approvingManager.Email;
            emailModel.ToName = approvingManager.FirstName + " " + approvingManager.LastName;
            emailModel.Subject = "Request to approve - Request #" + emailModel.RequestNum;

            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.OwnerNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendLocationPendingApprovalNotification(Site site, PortalUser requestor,
            Request request, AssetRequestDetail requestDetail)
        {
            var emailModel = new EmailTaskModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var assetManager = userManager.FindById(request.ApprovingManagerId);

            var locations = this.WorkContext.S.SiteService.GetLocations(site.Id);
            var locationOwners = "";
            foreach (var location in locations)
            {
                locationOwners += location.OwnerEmail + ", ";
            }

            Mapper.Map<Request, EmailTaskModel>(request, emailModel);
            emailModel.RequestId = requestor.Id;
            emailModel.AssetRequestDetail = requestDetailDto;
            emailModel.Request = request;

            emailModel.PrimaryImageUrl = string.Empty;
            if (requestDetailDto.Media != null & requestDetailDto.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + requestDetailDto.Media[0].FileName;
            }

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = locationOwners;
            emailModel.ToName = assetManager.FirstName + " " + assetManager.LastName;
            emailModel.Subject = "Request to approve - Request #" + emailModel.RequestNum;

            emailModel.CcAddress = assetManager.Email;

            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.LocationPendingApproval, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendLocationNotificationApproved(Site site, PortalUser requestor,
            Request request,
            AssetRequestDetail requestDetail)
        {
            var emailModel = new AssetApprovedEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            Mapper.Map<Request, AssetApprovedEmailModel>(request, emailModel);
            emailModel.RequestId = request.RequestNum;
            // emailModel.Notes = requestDetail.TaskComment;

            emailModel.CcAddress = requestor.Email;
            emailModel.CcName = requestor.FirstName + " " + requestor.LastName;
            emailModel.ToAddress = requestDetail.OwnerEmail;
            emailModel.ToName = requestDetail.OwnerName;
            emailModel.Subject = "Request approved - Request #" + emailModel.RequestNum;

            emailModel.Asset = requestDetail;
            emailModel.Request = request;
            emailModel.AssetRequestDetail = requestDetailDto;

            emailModel.PrimaryImageUrl = string.Empty;
            if (emailModel.Asset != null && emailModel.Asset.Media != null & emailModel.Asset.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + emailModel.Asset.Media[0].FileName;
            }

            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            var controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            var sender = new EmailSender<AssetApprovedEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.LocationNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendRequestorNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail)
        {
            switch (requestDetail.Status)
            {
                case GlobalConstants.RequestStatusTypes.Approved:
                    return DoSendAssetApprovedNotification(site, requestor, request, requestDetail).Result;
                case GlobalConstants.RequestStatusTypes.Denied:
                    return DoSendRequestDeniedNotification(site, requestor, request, requestDetail).Result;
            }

            return null;
        }


        public async Task<EmailResult> SendRequestorNotificationToOthers(Site site, PortalUser requestor,
            Request request, AssetRequestDetail requestDetail)
        {
            switch (requestDetail.Status)
            {
                case GlobalConstants.RequestStatusTypes.Approved:
                    return DoSendRequestApprovedToOthers(site, requestor, request, requestDetail).Result;
            }

            return null;
        }

        private string GetAdditonalApprovers(Site site)
        {
            var result = "";

            // Find users who are approvers
            var additionalApprovers = (from r in this.Repository.All<PortalUser>()
                where r.PortalId == site.Id && r.Roles.Contains("Approver")
                select r).ToList();
            foreach (var approver in additionalApprovers)
            {
                result += approver.Email + ", ";
            }

            return result;
        }

        public async Task<EmailResult> SendManagerPendingApproval(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail)
        {
            var emailModel = new EmailTaskModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            var userManager = IoC.Container.GetInstance<PortalUserService>();

            var requestorsManager = userManager.FindById(request.ApprovingManagerId);
            var additionalApprovers = GetAdditonalApprovers(site);

            Mapper.Map<Request, EmailTaskModel>(request, emailModel);
            emailModel.RequestId = requestor.Id;
            emailModel.AssetRequestDetail = requestDetailDto;
            emailModel.Request = request;

            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = requestorsManager.Email;
            if (!string.IsNullOrWhiteSpace(additionalApprovers))
                emailModel.CcAddress = additionalApprovers;
            emailModel.ToName = requestorsManager.FirstName + " " + requestorsManager.LastName;
            emailModel.Subject = "Request to approve - Request #" + emailModel.RequestNum;

            emailModel.PrimaryImageUrl = string.Empty;
            if (requestDetail != null && requestDetail.Media != null & requestDetail.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + requestDetail.Media[0].FileName;
            }

            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.ManagerNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendManagerPendingApproval(Site site, PortalUser requestor, Request request)
        {
            var emailModel = new EmailTaskModel(this.WorkContext.HttpContext, site.SiteSettings);
            emailModel.Request = request;

            Mapper.Map<Request, EmailTaskModel>(request, emailModel);
            emailModel.RequestId = requestor.Id;

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = request.ApprovingManagerEmail;
            emailModel.ToName = request.ApprovingManagerName;
            emailModel.Subject = "Request to approve - Request #" + emailModel.RequestNum;

            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.ManagerNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendWelcomeMessage(PortalUser user, Site site, string callbackUrl)
        {
            var emailModel = new WelcomeEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            emailModel.Site = site;
            emailModel.User = user;

            var controllerUrl = "/" + site.SiteSettings.PortalTag;
            if (!string.IsNullOrEmpty(callbackUrl))
                emailModel.Url = callbackUrl;
            else
            {
                var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
                emailModel.Url = "https://" + baseUrl + controllerUrl;
            }

            emailModel.ToAddress = user.Email;
            emailModel.ToName = user.FirstName + " " + user.LastName;
            emailModel.Subject = "Welcome to CARE";

            var sender = new EmailSender<WelcomeEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.WelcomeNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendWelcomeMessage4AdminUser(PortalUser user, Site site)
        {
            var emailModel = new WelcomeEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            emailModel.Site = site;
            emailModel.User = user;

            var controllerUrl = "/" + site.SiteSettings.PortalTag;
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.Url = "https://" + baseUrl + controllerUrl;
            emailModel.ToAddress = user.Email;
            emailModel.ToName = user.FirstName + " " + user.LastName;
            emailModel.Subject = "Welcome to CARE";

            var sender = new EmailSender<WelcomeEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.WelcomeNotification4AdminUser, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendResetPasswordNotification(PortalUser user, Site site, string callbackUrl)
        {
            var emailModel = new ForgotPasswordEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            emailModel.Site = site;
            emailModel.User = user;
            emailModel.CallbackUrl = callbackUrl;

            emailModel.ToAddress = user.Email;
            emailModel.ToName = user.FirstName + " " + user.LastName;
            emailModel.Subject = "Password reset code";

            var sender = new EmailSender<ForgotPasswordEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.ResetPasswordNotification, emailModel);

            return new EmailResult(); //todo: Return real error code        
        }

        public async Task<EmailResult> DoSendRequestApprovedToOthers(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail)
        {
            if (!site.SiteSettings.ApprovalCcAddresses.Any())
                return new EmailResult(); //todo: Return real error code;

            var emailModel = new EmailTaskModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            Mapper.Map<Request, EmailTaskModel>(request, emailModel);
            emailModel.RequestId = requestor.Id;
            emailModel.AssetRequestDetail = requestDetailDto;
            emailModel.Request = request;

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = GetToAddresses(site);
            emailModel.Subject = "Request approved - Request #" + emailModel.RequestNum;

            emailModel.PrimaryImageUrl = string.Empty;
            if (requestDetailDto.Media != null & requestDetailDto.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + requestDetailDto.Media[0].FileName;
            }

            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.RequestApprovedToOthers, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public string GetToAddresses(Site site)
        {
            var result = "";
            foreach (string address in site.SiteSettings.ApprovalCcAddresses)
            {
                result += address + ", ";
            }

            return result;
        }

        public async Task<EmailResult> DoSendAssetApprovedNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail)
        {
            var emailModel = new AssetApprovedEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            Mapper.Map<Request, AssetApprovedEmailModel>(request, emailModel);
            emailModel.RequestId = request.RequestNum;

            emailModel.CcAddress = requestor.Email;
            emailModel.CcName = requestor.FirstName + " " + requestor.LastName;
            emailModel.ToAddress = requestDetail.OwnerEmail;
            emailModel.ToName = requestDetail.OwnerName;
            emailModel.Subject = "Request approved - Request #" + emailModel.RequestNum;

            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            var controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;


            emailModel.Asset = requestDetail;
            emailModel.Request = request;
            emailModel.AssetRequestDetail = requestDetailDto;

            emailModel.PrimaryImageUrl = string.Empty;
            if (emailModel.Asset != null && emailModel.Asset.Media != null & emailModel.Asset.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + emailModel.Asset.Media[0].FileName;
            }

            var sender = new EmailSender<AssetApprovedEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.AssetApprovedNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> DoSendRequestDeniedNotification(Site site, PortalUser requestor, Request request,
            AssetRequestDetail requestDetail)
        {
            var emailModel = new AssetDeniedEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject =
                JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);
            requestDetailDto.CustomDataPairs = jsonObject;

            Mapper.Map<Request, AssetDeniedEmailModel>(request, emailModel);
            emailModel.RequestId = request.RequestNum;
            //  emailModel.Notes = requestDetail.TaskComment;

            emailModel.ToAddress = requestor.Email;
            emailModel.ToName = requestor.FirstName + " " + requestor.LastName;
            emailModel.CcAddress = request.ApprovingManagerEmail;
            emailModel.CcName = request.ApprovingManagerName;
            emailModel.Subject = "Request denied - Request #" + emailModel.RequestNum;

            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            var controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/" + requestDetail.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.Asset = requestDetail;
            emailModel.Request = request;

            emailModel.AssetRequestDetail = requestDetailDto;

            emailModel.PrimaryImageUrl = string.Empty;
            if (emailModel.Asset != null && emailModel.Asset.Media != null & emailModel.Asset.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/l/" + emailModel.Asset.Media[0].FileName;
            }

            var sender = new EmailSender<AssetDeniedEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.RequestDeniedNotification, emailModel);

            return new EmailResult(); //todo: Return real error code
        }


        //Send Weekly Uploaded Assets to a Portal-User
        public async Task<EmailResult> SendAssetUploadSummary(HttpContext cContext, Site site, PortalUser user,
            List<AssetsUploaded> assetsUploaded)
        {
            try
            {
                var emailModel = new AssetUploadSummaryModel(this.WorkContext.HttpContext, site.SiteSettings);

                emailModel.AssetsUploaded = assetsUploaded;
                emailModel.UserFirstName = user.FirstName;
                emailModel.UserLastName = user.LastName;
                emailModel.ToAddress = user.Email;
                emailModel.Subject = "Weekly Asset Summary";

                var sender = new EmailSender<AssetUploadSummaryModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.AssetUploadSummary, emailModel);

            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendPendingRequestReminder(PendingRequestReminderModel pendingReqReminderModel)
        {
            IoC.Container.GetInstance<IActivityLogService>()
                .LogActivity(GlobalConstants.ActivityTypes.SendPendingRequestReminder,
                    pendingReqReminderModel.PortalTag,
                    pendingReqReminderModel.Request.RequestorName,
                    pendingReqReminderModel.Request.RequestNum);

            try
            {
                var emailModel = pendingReqReminderModel;
                emailModel.Subject = "Pending Request Reminder";
                emailModel.ToAddress = emailModel.Request.ApprovingManagerEmail;

                var sender = new EmailSender<PendingRequestReminderModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.PendingRequestReminder, emailModel);
            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendRequestAssetNotAvailable(Site site, Request request, Asset asset)
        {
            try
            {
                var emailModel = new RequestAssetNotAvailableModel(this.WorkContext.HttpContext, site.SiteSettings);

                emailModel.Request = request;
                emailModel.ToAddress = request.RequestorEmail;
                emailModel.ToName = request.RequestorName;
                emailModel.Subject = "Request-Asset Not Available";

                emailModel.Asset = asset;

                emailModel.PrimaryImageURL = string.Empty;
                if (asset != null && asset.Media != null & asset.Media.Count > 0)
                {
                    emailModel.PrimaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                                 this.WorkContext.PortalTag + "/l/" + asset.Media[0].FileName;
                }

                var sender = new EmailSender<RequestAssetNotAvailableModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.RequestAssetNotAvailable, emailModel);

            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendWishListMatchedAssets(PortalUser user, List<AssetsUploaded> assetsUploaded,
            WishList wishList, HttpContext cContext, Site site)
        {
            try
            {
                var emailModel = new WishListMatchedAssetModel(this.WorkContext.HttpContext, site.SiteSettings);

                emailModel.AssetsUploaded = assetsUploaded;
                emailModel.UserFirstName = user.FirstName;
                emailModel.UserLastName = user.LastName;
                emailModel.ToAddress = user.Email;
                emailModel.WishListSearchCriteria = wishList.SearchCriteria;
                emailModel.Subject = "Wish matches: " + emailModel.WishListSearchCriteria;

                string matchedAssetsURL = string.Empty;
                string baseUrl = cContext.Request.Url.Authority;

                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
                {
                    baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
                }

                string controllerUrl = "/" + site.SiteSettings.PortalTag + "/WishList/WishListResult?wishListID=" +
                                       wishList.Id;
                matchedAssetsURL = "https://" + baseUrl + controllerUrl;

                emailModel.MatchedWishListURL = matchedAssetsURL;

                var sender = new EmailSender<WishListMatchedAssetModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.WishListMatchedAssets, emailModel);
            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendSoonToBeExpiringWishLists(HttpContext cContext, Site site, PortalUser user,
            List<WishList> wishlists)
        {
            try
            {
                var emailModel = new ExpiringWishListEmailModel(this.WorkContext.HttpContext, site.SiteSettings);

                emailModel.UserFirstName = user.FirstName;
                emailModel.UserLastName = user.LastName;
                emailModel.ToAddress = user.Email;
                emailModel.Subject = "Extend WishList before they expire.";

                emailModel.ExtendWishLists = new List<ExtendWishList>();

                List<WishList> wishLists = wishlists;

                string baseUrl = cContext.Request.Url.Authority;

                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
                {
                    baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
                }

                foreach (WishList cWishList in wishlists)
                {
                    string extendURL = string.Empty;

                    string controllerUrl = "/" + site.SiteSettings.PortalTag + "/WishList/Extend?wishListID=" +
                                           cWishList.Id + "&isAutoExtend=" + true;
                    extendURL = "https://" + baseUrl + controllerUrl;

                    ExtendWishList extWishList = new ExtendWishList();
                    extWishList.WishList = cWishList;
                    extWishList.ExtendURL = extendURL;

                    emailModel.ExtendWishLists.Add(extWishList);
                }

                var sender = new EmailSender<ExpiringWishListEmailModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.ExpiringWishList, emailModel);
            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendExpiringAssetsMessage(HttpContext cContext, Site site, PortalUser user,
            List<Asset> expiringAssets)
        {
            try
            {
                var emailModel = new ExpiringAssetsEmailModel(cContext, site.SiteSettings);

                emailModel.UserFirstName = user.FirstName;
                emailModel.UserLastName = user.LastName;
                emailModel.CompanyName = site.SiteSettings.CompanyName;
                emailModel.ExpiringAssets = new List<ExpiringAssetsEmailModel.AssetsExpiring>();
                emailModel.ToAddress = user.Email;
                emailModel.Subject = "Assets expiring today - " + site.SiteSettings.CompanyName;

                string baseUrl = cContext.Request.Url.Authority;

                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
                {
                    baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
                }

                foreach (Asset asset in expiringAssets)
                {
                    string assetURL = string.Empty;
                    string primaryImageURL = string.Empty;

                    string controllerUrl = "/" + site.SiteSettings.PortalTag + "/asset/index/" + asset.HitNumber;
                    assetURL = "https://" + baseUrl + controllerUrl;

                    if (asset != null && asset.Media != null && asset.Media.Count > 0)
                    {
                        primaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/" + site.SiteSettings.PortalTag +
                                          "/l/" + asset.Media[0].FileName;
                    }

                    var theAsset = new ExpiringAssetsEmailModel.AssetsExpiring();
                    theAsset.Asset = asset;
                    theAsset.AssetURL = assetURL;
                    theAsset.PrimaryImageURL = primaryImageURL;
                    theAsset.Asset.AvailForSale = theAsset.Asset.AvailForSale.ToLocalTime();

                    emailModel.ExpiringAssets.Add(theAsset);

                }

                var sender = new EmailSender<ExpiringAssetsEmailModel>();
                await sender.SendEmail(GlobalConstants.EmailTypes.ExpiringAssets, emailModel);
            }
            catch (Exception ex)
            {
                throw;
            }

            return new EmailResult();
        }

        public async Task<EmailResult> SendDraftAssetPendingApproval(Site site, DraftAsset draftAsset,
            HttpContext cContext)
        {
            var emailModel = new EmailTaskModel(cContext, site.SiteSettings);

            var admins = this.WorkContext.S.SiteService.GetAdmins(site.Id);
            var adminAddresses = "";
            foreach (var user in admins)
            {
                adminAddresses += user.Email + ", ";
            }

            if (!site.AccountExecutive.Email.IsNullOrWhiteSpace())
                adminAddresses = adminAddresses + site.AccountExecutive.Email;

            Mapper.Map<DraftAsset, EmailTaskModel>(draftAsset, emailModel);
            emailModel.DraftAsset = draftAsset;

            emailModel.PrimaryImageUrl = string.Empty;
            if (draftAsset.Media != null & draftAsset.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/drafts/" + draftAsset.OwnerId + "/t/" + draftAsset.Media[0].FileName;
            }

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/inbox";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.ApprovalURL = "https://" + baseUrl + controllerUrl;

            controllerUrl = "/" + this.WorkContext.PortalTag + "/drafts/index/" + draftAsset.HitNumber;
            emailModel.AssetURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = adminAddresses;
            emailModel.Subject = "Asset requires approval - Hit #" + draftAsset.HitNumber;


            var sender = new EmailSender<EmailTaskModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.DraftAssetPendingApproval, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public async Task<EmailResult> SendDraftAssetDeniedApproval(Site site, DraftAsset draftAsset, string message, HttpContext cContext)
        {
            var emailModel = new DraftAssetDeniedEmailModel(cContext, site.SiteSettings);

            Mapper.Map<DraftAsset, DraftAssetDeniedEmailModel>(draftAsset, emailModel);
            emailModel.DraftAsset = draftAsset;

            emailModel.PrimaryImageUrl = string.Empty;
            if (draftAsset.Media != null & draftAsset.Media.Count > 0)
            {
                emailModel.PrimaryImageUrl = "https://s3-us-west-1.amazonaws.com/hgpmedia/" +
                                             this.WorkContext.PortalTag +
                                             "/drafts/" + draftAsset.OwnerId + "/t/" + draftAsset.Media[0].FileName;
            }

            var controllerUrl = "/" + this.WorkContext.PortalTag + "/drafts";
            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            emailModel.DraftsURL = "https://" + baseUrl + controllerUrl;

            emailModel.ToAddress = draftAsset.OwnerEmail;
            emailModel.Subject = "Asset listing returned - Hit #" + draftAsset.HitNumber;


            var sender = new EmailSender<DraftAssetDeniedEmailModel>();
            await sender.SendEmail(GlobalConstants.EmailTypes.DraftAssetDeniedApproval, emailModel);

            return new EmailResult(); //todo: Return real error code
        }

        public void SendDraftAssetApprovedMessage(Site site, PortalUser assetOwner, DraftAsset draftAsset,
            HttpContext workContextHttpContext)
        {
        }
    }

}

internal class MyListener : TextWriterTraceListener
{
    public MyListener(string fileName)
        : base(fileName)
    {
    }

    public override void Write(string message, string category)
    {
        if (category == "Mail.dll")
            base.Write(message, category);
    }

    public override void WriteLine(string message, string category)
    {
        if (category == "Mail.dll")
            base.WriteLine(message, category);
    }
}
