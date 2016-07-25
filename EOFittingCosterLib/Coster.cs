using EvePasteNet;
using eZet.EveLib.EveCentralModule;
using System;
using System.Collections.Generic;
using System.Linq;
using eZet.EveLib.Core;
using EOFittingCosterLib.Models;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Text;
using EvePasteNet.Parsers.EFT.Models;
using System.IO;
using System.Globalization;

namespace EOFittingCosterLib
{
    public class Coster
    {
        public const string m_MissingAppraisals_3fields = "{0} item(s) could not be appraised.\r\n{1}";
        public const string m_NoErrors = "All items were successfully appraised.";

        public readonly string storageConStr = ConfigurationManager.ConnectionStrings["storageAccountConString"].ConnectionString;
        public readonly string containerName = ConfigurationManager.AppSettings.Get("containerName");
        public readonly string itemTypesBlobName = ConfigurationManager.AppSettings.Get("itemTypesBlobName");
        public readonly string cacheRelativeDirectory = ConfigurationManager.AppSettings.Get("cacheRelativeDirectory");

        public bool remoteStorageIsInitialized = false;
        public IList<Item> itemTypes;

        public Coster()
        {
            
        }

        public void InitialzeRemoteStorageAndRetrieveData()
        {
            List<Item> _itemTypes = new List<Item>();
            try
            {
                CloudStorageAccount storageAccount;
                if (!CloudStorageAccount.TryParse(storageConStr, out storageAccount))
                    throw new StorageException("Unable to connect to cloud storage.");
                var cloudClient = storageAccount.CreateCloudBlobClient();

                var cloudContainer = cloudClient.GetContainerReference(containerName);
                if (!cloudContainer.Exists())
                    throw new StorageException("Cloud container does not exist, therefore, itemTypes.json is not reachable.");

                var typesBlob = cloudContainer.GetBlockBlobReference(itemTypesBlobName);
                if (!typesBlob.Exists())
                    throw new StorageException("Blob:itemTypes.json does not exist in storage.");

                var jsonStr = typesBlob.DownloadText(Encoding.UTF8, null, null, null);
                _itemTypes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Item>>(jsonStr);
                CacheDocument(jsonStr);
                this.remoteStorageIsInitialized = true;
            }
            catch (Exception)
            {
                this.remoteStorageIsInitialized = false;
            }

            this.itemTypes = _itemTypes;
        }

        public bool CacheDocument(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                return false;

            try
            {
                var cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), cacheRelativeDirectory);
                if (!Directory.Exists(cacheDirectory))
                    Directory.CreateDirectory(cacheDirectory);

                var fileName = $"{ DateTime.UtcNow.ToString("MM_dd_yyyy") }.txt";
                var fullPath = Path.Combine(cacheDirectory, fileName);
                File.WriteAllText(fullPath, document, Encoding.UTF8);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public string RetrieveCachedDocument()
        {
            string cacheString = string.Empty;

            var cachedDirectory = Path.Combine(Directory.GetCurrentDirectory(), cacheRelativeDirectory);
            if (!Directory.Exists(cachedDirectory))
                return null;

            var files = Directory.GetFiles(cachedDirectory);
            if (files.Count() <= 0)
                return null;

            Tuple<DateTime, string> documentWithDate = new Tuple<DateTime, string>(DateTime.MinValue, "");
            for (int i = 0; i < files.Length; i++)
            {
                var _curCacheFile = files[i];
                DateTime _curCacheDate = ParseFileNameForDate(_curCacheFile).ToUniversalTime();
                try
                {
                    if (DateTime.UtcNow.AddDays(-1) > _curCacheDate || _curCacheDate <= DateTime.MinValue)
                    {
                        File.Delete(_curCacheFile);
                    }
                    else
                    {
                        cacheString = File.ReadAllText(_curCacheFile);
                        if (documentWithDate.Item1 < _curCacheDate)
                            documentWithDate = new Tuple<DateTime, string>(_curCacheDate, cacheString);
                    }
                }
                catch (Exception) { }
            }
            return documentWithDate.Item2;
        }

        public DateTime ParseFileNameForDate(string cachedFileFullPathString)
        {
            var fileInfo = new FileInfo(cachedFileFullPathString);
            var fileName = fileInfo.Name.Split('.')[0];
            DateTime dateTime = DateTime.MinValue;
            DateTime.TryParseExact(fileName, "MM_dd_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime);
            return dateTime;
        }

        /// <summary>
        /// All inclusive process which takes raw EFT text, parses and costs out each item and wraps this data into a response object.
        /// </summary>
        /// <param name="rawEFT"></param>
        /// <returns>CosterResponse</returns>
        public CosterResponse Costify(string rawEFT)
        {
            EvePaste paste = new EvePaste();
            EveCentral central = new EveCentral();

            // parse the eft text
            var parsed = paste.ParseEFT(rawEFT);

            // flatten the Ship object to List<Item>
            var flattened = FlattenShip(parsed);

            // retrieve cached file or get remote file
            itemTypes = RetrieveItemTypesData();

            // match typeIDs with typeNames
            var withTypeIDs = PopulateTypeIDs(flattened, itemTypes);

            // retrieve cost per unit
            var withCosts = PopulateCosts(withTypeIDs, central).Where(i => i.CostPU > 0);

            // sum it up and check to see if costify may be inaccurate
            var costerResponse = new CosterResponse();
            costerResponse.Sum = withCosts.Sum(c => c.CostPU * c.Quantity);
            costerResponse.AllItemsAppraised = flattened.Count() == withCosts.Count();
            var difference = flattened.Except(withCosts);
            costerResponse.Message = costerResponse.AllItemsAppraised
                ? m_NoErrors
                : string.Format(m_MissingAppraisals_3fields, flattened.Count() - withCosts.Count(), 
                    string.Join(Environment.NewLine, difference.Select(i => i.Name)));

            return costerResponse;
        }

        public List<Item> RetrieveItemTypesData()
        {
            var _itemTypes = new List<Item>();
            var dataString = RetrieveCachedDocument();
            if (string.IsNullOrWhiteSpace(dataString))
            {
                if (!this.remoteStorageIsInitialized)
                    InitialzeRemoteStorageAndRetrieveData();
            }
            else
            {
                _itemTypes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Item>>(dataString);
                if (_itemTypes?.Count() == 0)
                    throw new NullReferenceException(nameof(_itemTypes));
            }
            return _itemTypes;
        }

        public List<Item> PopulateCosts(List<Item> withTypeIDs, EveCentral central)
        {
            var typeIDs = withTypeIDs.Where(t => t.Id > 0).Select(t => t.Id).Distinct().ToArray();

            var centralOptions = new EveCentralOptions();
            centralOptions.Items = typeIDs;
            centralOptions.System = 30000142;
            var response = central.GetMarketStat(centralOptions);

            foreach (var item in withTypeIDs)
            {
                var match = response.Result.FirstOrDefault(i => i.TypeId == item.Id);
                if (match != null)
                    item.CostPU = match.SellOrders.Average;
            }

            var missingIds = withTypeIDs.Where(i => i.Id > 0 && i.CostPU == 0).Select(i => i.Id).Distinct().ToArray<int>();
            if (missingIds.Count() > 0)
            {
                centralOptions = new EveCentralOptions { Items = missingIds };
                response = central.GetMarketStat(centralOptions);
                foreach (var item in withTypeIDs)
                {
                    var match = response.Result.FirstOrDefault(i => i.TypeId == item.Id);
                    if (match != null)
                        item.CostPU = match.SellOrders.Average;
                }
            }
            return withTypeIDs;
        }
        public List<Item> PopulateTypeIDs(List<Item> flattened, IEnumerable<Item> itemTypes)
        {
            foreach (var item in flattened)
            {
                var dbItem = itemTypes.FirstOrDefault(it => it.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase));
                if (dbItem != null)
                    item.Id = dbItem.Id;
            }
            return flattened;
        }
        public List<Item> FlattenShip(Ship ship)
        {
            var flattened = new List<Item>();
            flattened.Add(new Item { Name = ship.Name, Quantity = 1 });
            flattened.AddRange(ship.Modules.Select(m=>new Item { Name = m.Name, Quantity = m.Quantity }));
            return flattened;
        }
    }
}
