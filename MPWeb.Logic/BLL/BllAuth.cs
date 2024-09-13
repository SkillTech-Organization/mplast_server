using AzureTableStore;
using MPWeb.Logic.Helpers;
using MPWeb.Logic.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MPWeb.Logic.BLL
{
    public class BllAuth
    {
        private string m_AuthTokenCryptAESKey;
        private string m_AuthTokenCryptAESIV;
        private string m_AuthTokenGenerationCryptAESKey;
        private string m_AuthTokenGenerationCryptAESIV;

        public BllAuth()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthTokenCryptAESKey"]))
            {
                throw new Exception("Parameter AuthTokenCryptAESKey is not set.");
            }
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthTokenCryptAESIV"]))
            {
                throw new Exception("Parameter AuthTokenCryptAESIV is not set.");
            }
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthTokenGenerationCryptAESKey"]))
            {
                throw new Exception("Parameter AuthTokenGenerationCryptAESKey is not set.");
            }
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthTokenGenerationCryptAESIV"]))
            {
                throw new Exception("Parameter AuthTokenGenerationCryptAESIV is not set.");
            }

            m_AuthTokenCryptAESKey = ConfigurationManager.AppSettings["AuthTokenCryptAESKey"];
            m_AuthTokenCryptAESIV = ConfigurationManager.AppSettings["AuthTokenCryptAESIV"];
            m_AuthTokenGenerationCryptAESKey = ConfigurationManager.AppSettings["AuthTokenGenerationCryptAESKey"];
            m_AuthTokenGenerationCryptAESIV = ConfigurationManager.AppSettings["AuthTokenGenerationCryptAESIV"];
        }

        public User AuthenticateUser(string username, string password)
        {
            var user = AzureAccess.Instance.Retrieve<PMUser>(PMUser.PartitonConst, username);

            if (user != null)
            {
                var aCH = new AuthCryptoHelper();
                if (aCH.VerifyPassword(password, user.Password))
                {
                    var ret = new User
                    {
                        Id = user.ID,
                        Name = user.UserName,
                        IsAuthenticated = true,
                        Roles = new List<string>
                            {
                                "User",
                                "RegisteredUser",
                                "GetTourData",
                                "GetVehicleData"
                            }
                    };
                    ret.Roles.Add(user.UserType.ToUpper());
                    return ret;
                }
            }

            return null;
        }

        public TemporaryUser AuthenticateUserByToken(string token)
        {
            var cryptoHelper = new AESCryptoHelper();
            var decryptedTokenText = cryptoHelper.DecryptString(token, m_AuthTokenCryptAESKey, m_AuthTokenCryptAESIV);
            var decryptedTokenContent = JsonConvert.DeserializeObject<List<PMTracedTour>>(decryptedTokenText);

            if (decryptedTokenContent == null)
            {
                return null;
            }
            if (decryptedTokenContent.Count == 0)
            {
                return null;
            }

            var pml = new BllPMLogin(Environment.MachineName);
            var loginDate = DateTime.UtcNow;
            var bllWTT = new BllWebTraceTour(Environment.MachineName);
            var tourList = bllWTT.RetrieveFullProcessedCachedList();

            foreach (var tT in decryptedTokenContent)
            {
                var tour = tourList.FirstOrDefault(x => x.ID == tT.TourID.ToString());
                if (tour != null)
                {
                    var tourPoint = tour.TourPoints.FirstOrDefault(x => x.TourID == tour.ID && x.Order == tT.Order);
                    if (tourPoint != null)
                    {
                            pml.MaintainItem(new PMLogin
                            {
                                Ticks = loginDate.Ticks.ToString(),
                                Date = String.Format("{0:yyyy.MM.dd}", loginDate),
                                DateTime = String.Format("{0:yyyy.MM.dd hh:mm:ss}", loginDate),
                                OrdNum = tourPoint.OrdNum.ToString(),
                                Name = tourPoint.Name,
                                Addr = tourPoint.Addr
                            });
                    }
                }
            }
            return CreateTemporaryUser(decryptedTokenContent);
        }

        public List<PMTracedTour> GetTempUATokenReqContent(string encodedTRContent)
        {
            var cryptoHelper = new AESCryptoHelper();
            var decryptedTRContent = cryptoHelper.DecryptString(encodedTRContent,
                m_AuthTokenGenerationCryptAESKey, m_AuthTokenGenerationCryptAESIV);

            if (decryptedTRContent == null)
            {
                return null;
            }

            var deserializedTRContent = JsonConvert.DeserializeObject<List<PMTracedTour>>(decryptedTRContent);

            return deserializedTRContent;
        }

        public string GetTemporaryUserAccessToken(List<PMTracedTour> vehicleList)
        {
            var tUser = CreateTemporaryUser(vehicleList);
            return GenerateTemporaryAccessToken(tUser);
        }

        private string GenerateTemporaryAccessToken(TemporaryUser tUser)
        {
            var cryptoHelper = new AESCryptoHelper();
            var rawToken = "";

            var vehicleIdListJSON = JsonConvert.SerializeObject(tUser.PMTracedTourList);
            rawToken = string.Concat(rawToken, vehicleIdListJSON);

            var encryptedToken = cryptoHelper.EncryptString(rawToken, m_AuthTokenCryptAESKey, m_AuthTokenCryptAESIV);
            return encryptedToken;
        }

        private TemporaryUser CreateTemporaryUser(List<PMTracedTour> tourPointList)
        {
            return new TemporaryUser
            {
                Id = new Guid().ToString(),
                IsAuthenticated = true,
                Name = "Ügyfél",
                Roles = new List<string>
                {
                    "User",
                    "TemporaryUser",
                    "GetTourData",
                    "GetVehicleData"
                },
                PMTracedTourList = tourPointList
            };
        }
    }
}
