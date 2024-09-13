using MPWeb.Logic.Cache;
using MPWeb.Logic.Helpers;
using MPWeb.Logic.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MPWeb.Logic.BLL
{
    public class BllWebVehicleTrace
    {
        private string m_vehicleTrackingService3DESKey;
        private string m_vehicleTrackingService3DESIV;
        private string m_vehicleTrackingServiceUrl;
        private string m_vehicleTrackingServiceUser;
        private string m_vehicleTrackingServicePassword;

        private HttpClient client = new HttpClient();

        public BllWebVehicleTrace()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["vehicleTrackingServiceUrl"]))
            {
                throw new ApplicationException("Parameter vehicleTrackingServiceUrl is not set.");
            }
            m_vehicleTrackingServiceUrl = ConfigurationManager.AppSettings["vehicleTrackingServiceUrl"];

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["vehicleTrackingServiceUser"]))
            {
                throw new ApplicationException("Parameter vehicleTrackingServiceUser is not set.");
            }
            m_vehicleTrackingServiceUser = ConfigurationManager.AppSettings["vehicleTrackingServiceUser"];

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["vehicleTrackingServicePassword"]))
            {
                throw new ApplicationException("Parameter vehicleTrackingServicePassword is not set.");
            }
            m_vehicleTrackingServicePassword = ConfigurationManager.AppSettings["vehicleTrackingServicePassword"];

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["vehicleTrackingService3DESKey"]))
            {
                throw new ApplicationException("Parameter vehicleTrackingService3DESKey is not set.");
            }
            m_vehicleTrackingService3DESKey = ConfigurationManager.AppSettings["vehicleTrackingService3DESKey"];

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["vehicleTrackingService3DESIV"]))
            {
                throw new ApplicationException("Parameter vehicleTrackingService3DESIV is not set.");
            }
            m_vehicleTrackingService3DESIV = ConfigurationManager.AppSettings["vehicleTrackingService3DESIV"];
        }

        public List<VehiclePositionData> RetriveCachedVehicleData(DateTime filterDate)
        {
            var vtd = new List<VehiclePositionData>();
            using (LockForCache lockObj = new LockForCache(VehicleTrackingCache.Locker))
            {
                vtd = JsonConvert.DeserializeObject<List<VehiclePositionData>>(JsonConvert.SerializeObject(
                    VehicleTrackingCache.Instance.CachedPositionData));
            }

            var bllWTT = new BllWebTraceTour(Environment.MachineName);
            var tourList = bllWTT.RetrieveFilteredCachedList(filterDate);

            var vehicleIdList = tourList.Select(x => x.TruckRegNo).Distinct();
            var retList = vtd.Where(x => vehicleIdList.Contains(x.Device)).ToList();

            return retList;
        }
        public List<VehiclePositionData> RetriveCachedVehicleDataByPMTourList(List<PMTour> tourList)
        {
            var vtd = new List<VehiclePositionData>();
            using (LockForCache lockObj = new LockForCache(VehicleTrackingCache.Locker))
            {
                vtd = JsonConvert.DeserializeObject<List<VehiclePositionData>>(JsonConvert.SerializeObject(
                    VehicleTrackingCache.Instance.CachedPositionData));
            }
            var vehicleIdList = tourList.Select(x => x.TruckRegNo).Distinct();
            var retList = vtd.Where(x => vehicleIdList.Contains(x.Device)).ToList();

            return retList;
        }

        public List<VehiclePositionData> RetriveCachedFilteredVehicleDataForTempUser(List<PMTracedTour> tourPointFilterList)
        {
            var bllWTT = new BllWebTraceTour(Environment.MachineName);

            var filterDate = DateTime.UtcNow.Date;
            var tourIdList = tourPointFilterList.Select(x => x.TourID).Distinct().ToList();
            var tmpTourList = bllWTT.RetrieveCachedListByTourID(tourIdList);

            var vehicleIdList = new List<string>();
            foreach (var t in tmpTourList)
            {
                vehicleIdList.Add(t.TruckRegNo);
            }
            var rawVehicleTrackingInfo = RetriveCachedVehicleDataByPMTourList(tmpTourList);
            var retList = rawVehicleTrackingInfo.Where(x => vehicleIdList.Contains(x.Device)).ToList();

            return retList;
        }
        
        public List<VehiclePositionData> GetVehicleTrackingInfo()
        {
            return GetVehiclePositionsFromService();
        }

 

        private List<string> GetVehicleList()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var passwd3DES = EncryptPassword3DES(m_vehicleTrackingServicePassword,
                Encoding.UTF8.GetBytes(m_vehicleTrackingService3DESKey),
                Encoding.UTF8.GetBytes(m_vehicleTrackingService3DESIV));
            var passwd3DESBase64 = Convert.ToBase64String(passwd3DES);

            var builder = new UriBuilder(m_vehicleTrackingServiceUrl + "api/" + m_vehicleTrackingServiceUser + "/vehicles");

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["password"] = passwd3DESBase64;
            builder.Query = query.ToString();
            string url = builder.ToString();

            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var res = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(res))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<List<string>>(res);
            }
            return null;
        }

        private List<VehiclePositionData> GetVehiclePositionsFromService()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var passwd3DES = EncryptPassword3DES(m_vehicleTrackingServicePassword,
                Encoding.UTF8.GetBytes(m_vehicleTrackingService3DESKey),
                Encoding.UTF8.GetBytes(m_vehicleTrackingService3DESIV));
            var passwd3DESBase64 = Convert.ToBase64String(passwd3DES);

            var builder = new UriBuilder(m_vehicleTrackingServiceUrl + "api/" + m_vehicleTrackingServiceUser + "/positions");

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["password"] = passwd3DESBase64;
            builder.Query = query.ToString();
            string url = builder.ToString();

            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var res = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(res))
                {
                    return new List<VehiclePositionData>();
                }

                return PreprocessVehiclePositionDataList(res);
            }
            return new List<VehiclePositionData>();
        }
        
        private List<VehiclePositionData> PreprocessVehiclePositionDataList(string rawResult)
        {
            var result = JsonConvert.DeserializeObject<List<VehiclePositionData>>(rawResult);
            
            // TODO additional processing here later on
            //

            return result;
        }

        private static byte[] EncryptPassword3DES(string password, byte[] tdesKey, byte[] tdesIV)
        {
            using (var tdes = new TripleDESCryptoServiceProvider())
            {
                tdes.Key = tdesKey;
                tdes.IV = tdesIV;
                tdes.Mode = CipherMode.CBC;

                using (var tdesEncryptor = tdes.CreateEncryptor())
                {
                    var pwdBytes = new ASCIIEncoding().GetBytes(password);
                    return tdesEncryptor.TransformFinalBlock(pwdBytes, 0, pwdBytes.Length);
                }
            }
        }
    }
}
