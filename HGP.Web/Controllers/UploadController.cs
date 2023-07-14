using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.WebPages;
using AutoMapper;
using AutoMapper.Internal;
using ExcelDataReader;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Services;
using HGP.Web.Utilities;
using Ionic.Zip;
using Microsoft.Win32.SafeHandles;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructureMap.TypeRules;
using WebGrease.Css.Extensions;

namespace HGP.Web.Controllers
{
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool IsRequired { get; set; }
        public int? MaxLength { get; set; }
    }
 
    public class ImportDatasetResult
    {
        public int AssetsImported { get; set; }
        public int DuplicateAssets { get; set; }
    }

    public class UploadControllerMappingProfile : Profile
    {
        public UploadControllerMappingProfile()
        {
            CreateMap<MediaFile, MediaFileDto>();
        }
    }

    [Authorize(Roles = "ClientAdmin, SuperAdmin")]
    public class UploadController : BaseController
    {
        private IAwsService AwsService;
        public static ILogger Logger { get; set; }
        private List<ColumnDefinition> ColumnDefinitions { get; set; }
        public static List<string> pathobj = new List<string>();
        public UploadController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("UploadController");
            this.AwsService = portalServices.AwsService;
        }

        [HttpPost]
        public ActionResult UploadLogo()
        {
            var site = this.S.SiteService.GetById(this.S.WorkContext.CurrentSite.Id);

            DoUploadLogo(site);

            return RedirectToRoute("PortalRoute", new { controller = "Settings", action = "Index" });
        }



        [HttpPost]
        public ActionResult UploadLogoAdmin(string siteId)
        {
            var site = this.S.SiteService.GetById(siteId);

            var result = DoUploadLogo(site);

            return RedirectToRoute("AdminPortalRoute", new { controller = "AdminSite", action = "Edit", id = site.SiteSettings.PortalTag });
        }

        [HttpPost]
        public bool DoUploadLogo(Site site)
        {
            if (Request.Files[0].HasFile())
            {
                HttpPostedFileBase file = Request.Files[0];

                var memoryStream = new MemoryStream();
                using (var br = new BinaryReader(file.InputStream))
                    memoryStream.Write(br.ReadBytes((int)file.InputStream.Length), 0, (int)file.InputStream.Length);

                var photosModel = new UploadMediaFilesModel();

                var extension = Path.GetExtension(file.FileName);
                var mediaFile = AttachContentFile(site, "logo" + extension, memoryStream, photosModel);

                site.SiteSettings.Logo = Mapper.Map<MediaFile, MediaFileDto>(mediaFile);
                this.S.SiteService.Save((Site)site);

                if (mediaFile != null)
                    TempData["message"] = string.Format("1 logo successfully uploaded.");

                return true; // todo: Return real error code

            }

            return false; // todo: Return real error code
        }

        [HttpPost]
        public ActionResult UploadImages()
        {
            try
            {
                if (!Request.Files[0].HasFile())
                    return RedirectToRoute("PortalRoute", new { controller = "AdminAssets", action = "Index" });

                foreach (string upload in Request.Files)
                {
                    if (!Request.Files[upload].HasFile())
                        continue;

                    var file = Request.Files[upload];

                    if (file == null)
                        continue;

                    if (file.ContentType == "application/x-zip-compressed" || file.ContentType == "application/zip" || file.ContentType == "application/x-gzip")
                    {
                        var count = ProcessZipFile(file);

                        if (count == 0)
                            if ((List<AlertMessage>)TempData["messages"] != null)
                                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Error, Message = string.Format("{0} {1} successfully uploaded.", count, count == 1 ? "image" : "images") });
                            else
                            if ((List<AlertMessage>)TempData["messages"] != null)
                                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = string.Format("{0} {1} successfully uploaded.", count, count == 1 ? "image" : "images") });
                    }
                    else
                    {
                        var memoryStream = new MemoryStream();

                        using (var br = new BinaryReader(file.InputStream))
                            memoryStream.Write(br.ReadBytes((int)file.InputStream.Length), 0,
                                (int)file.InputStream.Length);

                        var photosModel = new UploadMediaFilesModel();
                        var result = AttachFile(file.FileName, memoryStream, photosModel);

                        if (result == 0)
                            if ((List<AlertMessage>)TempData["messages"] != null)
                                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Error, Message = string.Format("No asset with a matching hit number was found {0}", file.FileName) });
                            else
                            if ((List<AlertMessage>)TempData["messages"] != null)
                                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = string.Format("1 image successfully uploaded.") });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception in UploadImages");
                throw;
            }


            return RedirectToRoute("PortalRoute", new { controller = "AdminAssets", action = "Index" });
        }

        private int ProcessZipFile(HttpPostedFileBase file)
        {
            Logger.Information(" Entering ProcessZipFile");

            var result = 0;
            var photosModel = new UploadMediaFilesModel();

            try
            {
                using (new TimedLogEntry("", "Processing zip file"))
                {
                    using (var zip = ZipFile.Read(file.InputStream))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.IsDirectory)
                                continue;

                            var memStream = new MemoryStream();

                            entry.Extract(memStream);

                            using (new TimedLogEntry("Attaching file {0} {1}", result + 1, entry.FileName))
                            {
                                result += AttachFile(entry.FileName, memStream, photosModel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception in ProcessZipFile");

                throw;
            }

            Logger.Information(" Leaving ProcessZipFile {0}", result);

            return result;
        }


        private MediaFile AttachContentFile(Site site, string fileName, MemoryStream memStream, UploadMediaFilesModel photosModel)
        {
            var mediaFile = new MediaFile()
            {
                ContentType = FileNameUtilities.GetContentTypeFromExtension(fileName),
                FileName = Path.GetFileName(fileName),
                FileData = memStream.ToArray(),
                SortOrder = 1,
                IsImage = FileNameUtilities.IsImageFromExtension(fileName)
            };

            this.AwsService.PutRootFile(site.SiteSettings.PortalTag, mediaFile.FileName,
                FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), memStream);

            return mediaFile; // todo: Return real error code
        }

        private int AttachFile(string fileName, MemoryStream memStream, UploadMediaFilesModel photosModel)
        {
            var result = 0;

            var hitNumber = FileNameUtilities.ExtractHitNumber(fileName);
            var sequenceNumber = FileNameUtilities.ExtractSequenceNumber(fileName);
            var site = this.S.WorkContext.CurrentSite;

            var targetAsset = this.S.AssetService.Repository.GetQuery<Asset>().AsQueryable().FirstOrDefault(a => a.PortalId == site.Id && hitNumber == a.HitNumber);
            if (targetAsset != null) // Make sure the target asset exists
            {
                var mediaFile = new MediaFile()
                {
                    ContentType = FileNameUtilities.GetContentTypeFromExtension(fileName),
                    FileName = Path.GetFileName(fileName),
                    FileData = memStream.ToArray(),
                    SortOrder = sequenceNumber,
                    IsImage = FileNameUtilities.IsImageFromExtension(fileName)
                };

                memStream.Seek(0, SeekOrigin.Begin);
                mediaFile.ThumbnailData = new ImageUtilities().GenerateThumbNail(memStream, ImageFormat.Jpeg, 0, 64).GetBuffer();
                memStream.Seek(0, SeekOrigin.Begin);
                mediaFile.LargeThumbnailData = new ImageUtilities().GenerateThumbNail(memStream, ImageFormat.Jpeg, 0, 225).GetBuffer();

                this.AwsService.PutFile(site.SiteSettings.PortalTag, "i", mediaFile.FileName, FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), memStream);
                this.AwsService.PutFile(site.SiteSettings.PortalTag, "t", mediaFile.FileName, FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), new MemoryStream(mediaFile.ThumbnailData));
                this.AwsService.PutFile(site.SiteSettings.PortalTag, "l", mediaFile.FileName, FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), new MemoryStream(mediaFile.LargeThumbnailData));

                var mediaFileDto = Mapper.Map<MediaFile, MediaFileDto>(mediaFile);
                var existingMediaFiles = targetAsset.Media.Where(x => x.FileName.ToLower() == mediaFileDto.FileName.ToLower());
                if (!existingMediaFiles.Any())
                {
                    this.S.AssetService.AttachMedia(targetAsset, mediaFileDto);
                    result++;
                }           
            }
            else
                Logger.Information(" Asset not found {0}", hitNumber);

            return result;
        }


        [HttpPost]
        public ActionResult UploadAssets(HttpPostedFileBase fileData)
        {
            if (!fileData.HasFile()) return Content("File upload failed!");

            var result = DoUploadExcelFile(this.S.WorkContext.CurrentSite.Id, fileData.InputStream, fileData.FileName, fileData.ContentType);

            return result;
        }

        [HttpPost]
        public ActionResult TryUploadAssetsAgain(string fileName, string fullFileName, string siteId, string contentType)
        {
            byte[] data;

            using (FileStream stream = new FileStream(fullFileName, FileMode.Open, FileAccess.Read))
            {
                var size = (int)stream.Length; // Returns the length of the file
                data = new byte[size]; // Initializes and array in which to store the file
                stream.Read(data, 0, size);
                return DoUploadExcelFile(siteId, stream, fileName, contentType);
            }
        }

        [HttpPost]
        public ActionResult DoUploadAssets(ReviewAssetUploadModel model)
        {
            var currentUser = this.S.WorkContext.CurrentUser.Id;
            ImportExcelData(model, true, currentUser);

            switch (model.ImportResult.AssetsImported)
            {
                case 0:
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Warning, Message = "No new assets were uploaded." });
                    break;

                case 1:
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "1 asset successfully imported. This asset is hidden from view until you mark it visible." });
                    break;

                default:
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = string.Format("{0} assets successfully imported.", model.ImportResult.AssetsImported) });
                    break;
            }

            switch (model.ImportResult.DuplicateAssets)
            {
                case 0:
                    // No message required
                    break;
                case 1:
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Warning, Message = "1 asset already uploaded." });
                    break;

                default:
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Warning, Message = string.Format("{0} assets skipped. They were previously uploaded.", model.ImportResult.DuplicateAssets) });
                    break;
            }

            return RedirectToRoute("PortalRoute", new { controller = "AdminAssets", action = "Index" });
        }



        public static void CopyStream(Stream input, Stream output)
        {
            input.CopyTo(output);
        }

        public ActionResult DoUploadExcelFile(string siteId, Stream fileStream, string fileName, string contentType)
        {
            var portalModel = this.S.WorkContext.CurrentSite;
            var user = this.S.WorkContext.CurrentUser;
            string path =
                Path.Combine(string.Format("c:\\windows\\temp\\care\\{0}\\{1}\\assets\\",
                    portalModel.SiteSettings.PortalTag, user.Id));
            Directory.CreateDirectory(path);

            path = path + Path.GetRandomFileName() + Path.GetExtension(fileName);
            var errorFilePath = path + Path.GetRandomFileName() + Path.GetExtension(fileName) + "_errors.csv";

            var newStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.CopyTo(newStream);
            fileStream.Seek(0, SeekOrigin.Begin);
            newStream.Close();
            newStream.Dispose();
            fileStream.Close();
            fileStream.Dispose();

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ReviewAssetUploadModel>();
            model.FileName = fileName;
            model.FullFileName = path;
            model.SiteId = siteId;
            model.ContentType = contentType;

            var currentUser = this.S.WorkContext.CurrentUser.Id;
            ImportExcelData(model, false, currentUser);

            if ((model.Errors != null) && (model.Errors.Count > 0))
            {
                if (model.Errors.Count > 100)
                    model.Errors = model.Errors.Take(100).ToList();
                var header = "Line,Severity,Location,Data\n";
                var values = header + model.Errors.ToArray().AsCsv();
                using (StreamWriter sw = System.IO.File.CreateText(errorFilePath))
                {
                    sw.Write(values);
                }

                model.ErrorFullFileName = errorFilePath;
            }

            model.JsonData = JsonConvert.SerializeObject(model);
            return View("~/Views/AdminAssets/ReviewUpload.cshtml", model);

            return RedirectToRoute("PortalRoute", new {controller = "AdminAssets", action = "Index"});
        }

        private void ImportExcelData(ReviewAssetUploadModel model, bool doImport, string importingUserId)
        {
            this.ColumnDefinitions = new List<ColumnDefinition>();
            ConfigureColumns();

            var dataSet = ImportFromExcelFile(model.FullFileName);

            model.Columns = dataSet.Tables[0].Columns.Count;
            model.Rows = dataSet.Tables[0].Rows.Count;

            var okayToContinue = CheckForMissingColumns(dataSet, model.Errors);

            AddMissingColumns(dataSet);

            okayToContinue = CheckForBadData(dataSet, model.Errors);

            var duplicateCount = CheckForDuplicateHitNumbers(dataSet, model.Errors);

            if (okayToContinue || doImport)
                okayToContinue = AssignDefaults(model.SiteId, dataSet, model.Errors);

            if (okayToContinue || doImport)
                okayToContinue = ConfirmLocation(dataSet, model.Errors);

            if (okayToContinue || doImport)
                okayToContinue = AssignOwnerIds(dataSet, model.Errors);

            if ((okayToContinue && doImport) || (doImport))
            {
                PruneErrorRows(dataSet, model);
                model.ImportResult = ImportDataset(model.SiteId, dataSet, model.Errors, importingUserId);

                // Always update counts, doesn't hurt
                this.S.SiteService.UpdateCategories(model.SiteId);
                this.S.SiteService.UpdateManufacturers(model.SiteId);
                
            }

            model.Errors = model.Errors.OrderBy(x => x.LineNumber).ToList();

            return;
        }

        private bool AssignOwnerIds(DataSet dataSet, IList<FileParseError> errors)
        {
            var okayToContinue = true;

            // Do we even have an OwnerEmail column?
            if (dataSet.Tables[0].Columns.Contains("OwnerEmail"))
            {
                // Get a list of location ids for this portal
                var portalModel = this.S.WorkContext.CurrentSite;
                var owners = this.S.SiteService.GetOwners(portalModel.Id);

                var rowNumber = 0;
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    rowNumber++;

                    var ownerEmail = row["OwnerEmail"].ToString().Trim().ToLower();
                    if (ownerEmail.IsEmpty())
                        continue; // Email is optional, skip to next row

                    if (owners.Any(x => x.Email.ToLowerInvariant() == ownerEmail))
                    {
                        row["OwnerId"] = owners.First(x => x.Email.ToLowerInvariant() == ownerEmail).Id;
                    }
                    else
                    {
                        errors.Add(new FileParseError()
                        {
                            Message = "Owner not found",
                            Severity = FileParseErrorSeverity.Error,
                            Data = row["OwnerEmail"].ToString(),
                            LineNumber = rowNumber + 1, // Add one for header row
                            ErrorType = FileParseErrorType.OwnerNotFound
                        });
                        okayToContinue = false;
                    }
                }
                
            }
            return okayToContinue;
        }

        private int CheckForDuplicateHitNumbers(DataSet dataSet, IList<FileParseError> errors)
        {
            var duplicateCount = 0;
            var duplicateHitNumbers = new List<string>();

            // Get a list of location ids for this portal
            var portalModel = this.S.WorkContext.CurrentSite;
            List<string> hitNumbers = this.S.SiteService.GetHitNumbers(portalModel.Id);

            var rowNumber = 0;
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                rowNumber++;

                string hitNumber = row["HitNumber"].ToString().Trim().ToLower();

                if (hitNumbers.Contains(hitNumber))
                {
                    duplicateCount++;

                    errors.Add(new FileParseError()
                    {
                        Message = "Asset exists and will be skipped",
                        Severity = FileParseErrorSeverity.Warning,
                        Data = hitNumber,
                        LineNumber = rowNumber + 1, // Add one for header row
                        ErrorType = FileParseErrorType.General
                    });
                }
            }
            return duplicateCount;
        }

        private void PruneErrorRows(DataSet dataSet, ReviewAssetUploadModel model)
        {
            for (int i = dataSet.Tables[0].Rows.Count - 1; i >= 0; i--)
            {
                var targetLineNumber = i + 2; // +1 for zero based count, +1 for header row
                if (model.Errors.Any(x => x.LineNumber == targetLineNumber && x.Severity == FileParseErrorSeverity.Error))
                {
                    dataSet.Tables[0].Rows.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Creates a set of minimum columns that must be in the spreadsheet and
        /// creates columns for custom fields
        /// </summary>
        private void ConfigureColumns()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Id", Type = typeof(int), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "HitNumber", Type = typeof(string), IsRequired = true });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "ClientIdNumber", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Title", Type = typeof(string), IsRequired = true, MaxLength = 150 });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Description", Type = typeof(string), IsRequired = false, MaxLength = 10000 });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Location", Type = typeof(string), IsRequired = true, MaxLength = 255 });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Category", Type = typeof(string), IsRequired = true });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "ImportBatchId", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "BookValue", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "SerialNumber", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "ModelNumber", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "Manufacturer", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "OwnerEmail", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "OwnerId", Type = typeof(string), IsRequired = false });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Name = "DisplayBookValue", Type = typeof(bool), IsRequired = false });
        }

        private bool RowHasData(DataRow row)
        {
            var foundData = false;

            foreach (var dataField in row.ItemArray)
            {
                if (!string.IsNullOrWhiteSpace(dataField.ToString()))
                    return true;
            }

            return foundData;
        }

        private static DataSet ImportFromExcelFile(string filePath)
        {
            FileStream stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read);
            var size = (int)stream.Length; // Returns the length of the file
            var data = new byte[size]; // Initializes and array in which to store the file
            stream.Read(data, 0, size);
            stream.Close();
            stream.Dispose();

            var ms = new MemoryStream(data);


            IExcelDataReader excelReader = null;
            DataSet ds;
            if (filePath.ToLower().Contains(".xlsx"))
            {
                excelReader = ExcelReaderFactory.CreateReader(ms);
                ds = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                excelReader.Close();
            }
            else
            {
                //regular .xls files
                excelReader = ExcelReaderFactory.CreateBinaryReader(ms);
                ds = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });
                excelReader.Close();
            }

            // Rename the table to Assets so Ado.Net can import the data
            if ((ds != null) && (ds.Tables[0] != null))
            {
                ds.Tables[0].TableName = "Assets";
            }

            ms.Close();
            ms.Dispose();

            return ds;
        }

        /// <summary>
        /// Make sure one of the portal's locations is specified for each row
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private bool ConfirmLocation(DataSet dataSet, IList<FileParseError> errors)
        {
            var okayToContinue = true;

            // Get a list of location ids for this portal
            var portalModel = this.S.WorkContext.CurrentSite;
            var locations = this.S.SiteService.GetLocations(portalModel.Id);

            var locationDict = locations.ToDictionary(locationModel => locationModel.Name.ToLower(), locationModel => locationModel.Name);

            var rowNumber = 0;
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                rowNumber++;

                var locationName = row["Location"].ToString().Trim().ToLower();

                if (locationDict.ContainsKey(locationName))
                {
                    row["Location"] = locationDict[locationName];
                }
                else
                {
                    errors.Add(new FileParseError()
                    {
                        Message = "Location not found",
                        Severity = FileParseErrorSeverity.Error,
                        Data = row["Location"].ToString(),
                        LineNumber = rowNumber + 1, // Add one for header row
                        ErrorType = FileParseErrorType.MissingLocation
                    });
                    okayToContinue = false;
                }
            }
            return okayToContinue;
        }

        /// <summary>
        /// Make sure a valid book value is specified
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private bool CheckBookValues(DataSet dataSet, IList<FileParseError> errors)
        {
            var hasBadData = false;

            var rowNumber = 0;
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                rowNumber++;

                var bookValue = row["BookValue"].ToString();
                decimal  decimalValue = 0;
                if (!decimal.TryParse(bookValue, out decimalValue))
                {
                    errors.Add(new FileParseError()
                    {
                        Message = "Invalid book value",
                        Severity = FileParseErrorSeverity.Error,
                        Data = row["BookValue"].ToString(),
                        LineNumber = rowNumber + 1, // Add one for header row
                        ErrorType = FileParseErrorType.BadValue
                    });
                    hasBadData = true;
                }
            }
            return hasBadData;
        }

        /// <summary>
        /// Checks to see if any fields that are required are in the spreadsheet. Also
        /// checks custom fields.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private bool CheckForMissingColumns(DataSet dataSet, IList<FileParseError> errors)
        {
            var okayToContinue = true;

            foreach (var columnDefinition in this.ColumnDefinitions)
            {
                if (!dataSet.Tables[0].Columns.Contains(columnDefinition.Name))
                {
                    if (columnDefinition.IsRequired)
                    {
                        errors.Add(new FileParseError()
                        {
                            Message = string.Format("A required column ({0}) is missing", columnDefinition.Name),
                            Severity = FileParseErrorSeverity.Error
                        });
                        okayToContinue = false;
                    }
                }
            }

            return okayToContinue;
        }

        /// <summary>
        /// Add columns that are internal and not always in the spreadsheet. Every field in the database 
        /// must be in the DataSet.
        /// </summary>
        /// <param name="dataSet"></param>
        private void AddMissingColumns(DataSet dataSet)
        {
            // Build list of properties
            var protoAsset = new Asset();
            var properties = protoAsset.GetType().GetProperties();

            // Add missing properties
            foreach (var propertyInfo in properties)
            {
                if (!dataSet.Tables[0].Columns.Contains(propertyInfo.Name) && (!propertyInfo.PropertyType.IsNullable())) // todo: May be an error, need to test
                    dataSet.Tables[0].Columns.Add(new DataColumn(propertyInfo.Name, propertyInfo.PropertyType));
            }

            if (!dataSet.Tables[0].Columns.Contains("Id"))
                dataSet.Tables[0].Columns.Add(new DataColumn("Id", typeof(string)));
            if (!dataSet.Tables[0].Columns.Contains("OwnerEmail"))
                dataSet.Tables[0].Columns.Add(new DataColumn("OwnerEmail", typeof(string)));
        }


        private bool CheckForBadData(DataSet dataSet, IList<FileParseError> errors)
        {
            var okayToContinue = true;
            var rowCounter = 0;

            //var portalModel = ContextCache<PortalModel>.Get(ContextCache.PortalModelSessionCache);

            foreach (var columnDefinition in this.ColumnDefinitions)
            {
                #region Check required fields
                if (columnDefinition.IsRequired)
                {
                    rowCounter = 0;
                    foreach (DataRow row in dataSet.Tables[0].Rows)
                    {
                        // Skip empty rows
                        if (RowHasData(row))
                        {
                            rowCounter++;
                            if (row[columnDefinition.Name].ToString().Length == 0)
                            {
                                errors.Add(new FileParseError()
                                {
                                    Message =
                                        string.Format("{0} is required. Please enter a value.",
                                                      columnDefinition.Name),
                                    Severity = FileParseErrorSeverity.Error,
                                    LineNumber = rowCounter + 1,
                                    // Add one because the spreadsheet has a header
                                });
                                okayToContinue = false;
                            }
                        }
                    }
                }
                #endregion

                #region Check string lengths
                if (columnDefinition.Type == typeof(string))
                {
                    rowCounter = 0;
                    foreach (DataRow row in dataSet.Tables[0].Rows)
                    {
                        rowCounter++;
                        if (row.ItemArray.Contains(columnDefinition.Name))
                        {
                            var dataLength = row[columnDefinition.Name].ToString().Length;
                            if (dataLength > columnDefinition.MaxLength)
                            {
                                row[columnDefinition.Name] = row[columnDefinition.Name].ToString().Substring(0, columnDefinition.MaxLength.Value);
                                errors.Add(new FileParseError()
                                {
                                    Message = string.Format("Data is too long and will be truncated ({0})", columnDefinition.Name),
                                    Severity = FileParseErrorSeverity.Warning,
                                    LineNumber = rowCounter + 1, // Add one because the spreadsheet has a header
                                    Data = string.Format("Maximum: {0}, Your data: {1}", columnDefinition.MaxLength, dataLength)
                                });
                            }
                        }

                    }
                }
                #endregion
            }

            var badValues = CheckBookValues(dataSet, errors);
            if (okayToContinue && badValues)
                okayToContinue = false; // Don't let okayToContinue get set to true if it is already false

            return okayToContinue;
        }

        /// <summary>
        /// Assign some rational defaults for fields that must have initial values
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="catalog"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private bool AssignDefaults(string siteId, DataSet dataSet, IList<FileParseError> errors)
        {
            var okayToContinue = true;

            var user = this.S.WorkContext.CurrentUser;

            var batchId = new Random().Next(100000, int.MaxValue);

            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                row["Quantity"] = 1;
                row["Status"] = (int)GlobalConstants.AssetStatusTypes.Available;
                row["IsVisible"] = true; // todo: Change to false
                row["PortalId"] = siteId;
                if (string.IsNullOrWhiteSpace(row["OwnerEmail"].ToString()))
                    row["OwnerEmail"] = user.Email;
                if (string.IsNullOrWhiteSpace(row["Catalog"].ToString()))
                    row["Catalog"] = "Default";
                if (string.IsNullOrWhiteSpace(row["BookValue"].ToString()))
                    row["BookValue"] = "0";
                if (string.IsNullOrWhiteSpace(row["DisplayBookValue"].ToString()))
                    row["DisplayBookValue"] = true;

                if (string.IsNullOrWhiteSpace(row["ImportBatchId"].ToString()))
                    row["ImportBatchId"] = batchId;

                if (string.IsNullOrWhiteSpace(row["AvailForRedeploy"].ToString()))
                    row["AvailForRedeploy"] = DateTime.Now;
                if (string.IsNullOrWhiteSpace(row["AvailForSale"].ToString()))
                    row["AvailForSale"] = DateTime.Now.AddDays(90);

                row["CreatedDate"] = DateTime.Now;
                row["UpdatedDate"] = DateTime.Now;
            }

            return okayToContinue;
        }


        private ImportDatasetResult ImportDataset(string siteId, DataSet dataSet, IList<FileParseError> errors, string importingUserId)
        {
            var duplicateAssets = 0;
            var assetsImported = 0;

            var assetService = this.S.AssetService;
            try
            {
                // Import the asset records
                var assets = GetDataFromDataTable(dataSet, "Assets", importingUserId);
                foreach (var asset in assets)
                {
                    // Does the asset already exist?
                    var existingAsset = assetService.GetByHitNumber(siteId, asset.HitNumber);
                    if (existingAsset == null)
                    {
                        assetService.Save(asset);
                        assetsImported++;                        
                    }
                    else
                    {
                        duplicateAssets++;
                    }
                }

            }
            catch (Exception ex)
            {
                errors.Add(new FileParseError()
                {
                    Severity = FileParseErrorSeverity.Fatal,
                    Message = string.Format("An exception occurred during asset import. {0}", ex.Message)
                });
            }

            return new ImportDatasetResult { DuplicateAssets = duplicateAssets, AssetsImported = assetsImported };
        }

        private string ParseString(Object drObject)
        {
            return drObject.ToString().Trim();
        }

        private DateTime ParseDateTime(Object drObject)
        {
            // No need to adjust time, local time will be converted to UTC by the Mongo driver
            return DateTime.Parse(drObject.ToString());
        }

        private int ParseInt(Object drObject)
        {
            var result = 0;

            var sourceData = drObject.ToString();
            var resultInt = 1;
            var tryResult = int.TryParse(sourceData, out resultInt);
            if (tryResult.HasValue())
                result = resultInt;
            else
                result = 1;

            return result;
        }

        private bool ParseBool(Object drObject)
        {
            var result = false;

            string boolStr = drObject.ToString().ToLowerInvariant().Trim();
            switch (boolStr)
            {
                case "true":
                case "yes":
                    result = true;
                    break;
            }

            return result;
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        private IList<Asset> GetDataFromDataTable(DataSet dataSet, string tableName, string importingUserId)
        {
            var table = dataSet.Tables[tableName];
            List<Asset> result = new List<Asset>();

            var customColumns = table.Columns.Cast<DataColumn>().Where(x => x.ColumnName.ToLowerInvariant().StartsWith("custom"));

            foreach (DataRow row in dataSet.Tables[tableName].Rows)
            {
                var asset = new Asset();
                // Assign default values
                asset.Status = ParseEnum<GlobalConstants.AssetStatusTypes>(row["Status"].ToString());
                asset.IsVisible = ParseBool(row["IsVisible"]);
                asset.PortalId = ParseString(row["PortalId"]);
                asset.OwnerId = ParseString(row["OwnerId"]);
                asset.ImportBatchId = ParseString(row["ImportBatchId"]);
                asset.Quantity = 1;
                asset.IsFromDraftAsset = false;

                asset.CreatedDate = asset.UpdatedDate = DateTime.Now;
                asset.CreatedBy = importingUserId;

                asset.HitNumber = ParseString(row["HitNumber"]);
                asset.ClientIdNumber = ParseString(row["ClientIdNumber"]);
                asset.Title = ParseString(row["Title"]);
                asset.IsVisible = ParseBool(row["IsVisible"]);
                asset.Description = ParseString(row["Description"]);
                asset.Manufacturer = ParseString(row["Manufacturer"]);
                asset.ModelNumber = ParseString(row["ModelNumber"]);
                asset.SerialNumber = ParseString(row["SerialNumber"]);
                asset.DisplayBookValue = ParseBool(row["DisplayBookValue"]);
                asset.BookValue = ParseString(row["BookValue"]);
                asset.ModelNumber = ParseString(row["ModelNumber"]);
                asset.Location = ParseString(row["Location"]);
                asset.ServiceStatus = ParseString(row["ServiceStatus"]);
                asset.Condition = ParseString(row["Condition"]);
                asset.Category = ParseString(row["Category"]);
                asset.Catalog = ParseString(row["Catalog"]);
                asset.AvailForRedeploy = ParseDateTime(row["AvailForRedeploy"]);
                asset.AvailForSale = ParseDateTime(row["AvailForSale"]);

                if (customColumns.Any())
                {
                    var jsonArray = new List<object>();
                    foreach (var customColumn in customColumns)
                    {
                        if (!string.IsNullOrEmpty(row[customColumn.ColumnName].ToString()))
                        {
                            var value = row[customColumn.ColumnName].ToString().Trim();
                            var key =
                                customColumn.ColumnName.Substring(
                                    customColumn.ColumnName.IndexOf(":", System.StringComparison.Ordinal) + 1).Trim();
                            var obj = new {key = key, value = value};
                            jsonArray.Add(obj);
                        }
                    }

                    var serializer = new JavaScriptSerializer();
                    asset.CustomData = serializer.Serialize(jsonArray);
                }
                result.Add(asset);
            }

            ////todo: Convert book value manually until converter is working
            //for (int i = 0; i < table.Rows.Count; i++)
            //{
            //    result[i].HitNumber = ParseString(table.Rows[i]["HitNumber"]);
            //    result[i].ClientIdNumber = ParseString(table.Rows[i]["ClientIdNumber"]);
            //    result[i].Title = ParseString(table.Rows[i]["Title"]);
            //    result[i].IsVisible = ParseBool(table.Rows[i]["IsVisible"]);
            //    result[i].Description = ParseString(table.Rows[i]["Description"]);
            //    result[i].Manufacturer = ParseString(table.Rows[i]["Manufacturer"]);
            //    result[i].ModelNumber = ParseString(table.Rows[i]["ModelNumber"]);
            //    result[i].SerialNumber = ParseString(table.Rows[i]["SerialNumber"]);
            //    result[i].DisplayBookValue = ParseBool(table.Rows[i]["DisplayBookValue"]);
            //    result[i].BookValue = ParseString(table.Rows[i]["BookValue"]);
            //    result[i].ModelNumber = ParseString(table.Rows[i]["ModelNumber"]);
            //    result[i].Location = ParseString(table.Rows[i]["Location"]);
            //    result[i].ServiceStatus = ParseString(table.Rows[i]["ServiceStatus"]);
            //    result[i].Condition = ParseString(table.Rows[i]["Condition"]);
            //    result[i].Category = ParseString(table.Rows[i]["Category"]);
            //    result[i].Catalog = ParseString(table.Rows[i]["Catalog"]);
            //    result[i].AvailForRedeploy = ParseDateTime(table.Rows[i]["AvailForRedeploy"]);
            //    result[i].AvailForSale = ParseDateTime(table.Rows[i]["AvailForSale"]);

            //    if (customColumns.Any())
            //    {
            //        var jsonArray = new List<object>();
            //        foreach (var customColumn in customColumns)
            //        {
            //            if (!string.IsNullOrEmpty(table.Rows[i][customColumn.ColumnName].ToString()))
            //            {
            //                var value = table.Rows[i][customColumn.ColumnName].ToString().Trim();
            //                var key = customColumn.ColumnName.Substring(customColumn.ColumnName.IndexOf(":", System.StringComparison.Ordinal) + 1).Trim();
            //                var obj = new { key = key, value = value };
            //                jsonArray.Add(obj);
            //            }
            //        }

            //        var serializer = new JavaScriptSerializer();
            //        result[i].CustomData = serializer.Serialize(jsonArray);
            //    }
            //}

            return result;
        }
        public ActionResult Upload()
        {
            bool isSavedSuccessfully = true;
            string fName = "";
            try
            {
                foreach (string fileName in Request.Files)
                {
                    HttpPostedFileBase file = Request.Files[fileName];
                    fName = file.FileName;
                    if (file != null && file.ContentLength > 0)
                    {
                        var guid = new Guid().ToString().Substring(0,4);
                        var path = Path.Combine(Server.MapPath("~/MyImages"));
                        string pathString = System.IO.Path.Combine(path.ToString());
                        var fileName1 = Path.GetFileName(file.FileName);
                        bool isExists = System.IO.Directory.Exists(pathString);
                        if (!isExists) System.IO.Directory.CreateDirectory(pathString);
                        var uploadpath1 = string.Format("{0}\\{1}", pathString, file.FileName+guid);
                        var uploadpath = string.Format("{0}",file.FileName+ guid);
                        file.SaveAs(uploadpath1);
                        pathobj.Add(uploadpath);
                    }
                    TempData["Path"] = pathobj;
                }
            }
            catch (Exception ex)
            {
                isSavedSuccessfully = false;
            }
            if (isSavedSuccessfully)
            {
                return Json(new
                {
                    Message = fName
                });
            }
            else
            {
                return Json(new
                {
                    Message = "Error in saving file"
                });
            }
        }
    }
    //public static class StrConverter
    //{
    //    public static void ReadAsString(this IMemberConfigurationExpression<IDataReader> opt, string fieldName)
    //    {
    //        opt.MapFrom(reader => reader.GetDecimal(reader.GetOrdinal(fieldName)).ToString("#0"));
    //    }
    //}

    //public class DecimalToCurrencyConverter : ITypeConverter<double, string>
    //{
    //    public string Convert(ResolutionContext context)
    //    {
    //        var source = (double)context.SourceValue;
    //        var result = source.ToString();
    //        return result;
    //    }
    //}

    //public class CurrencyToDecimalConverter : ITypeConverter<string, double>
    //{
    //    public double Convert(ResolutionContext context)
    //    {
    //        var source = (string)context.SourceValue;
    //        return (double.Parse(source));
    //    }
    //}


}

