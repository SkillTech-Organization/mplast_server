using Microsoft.VisualStudio.TestTools.UnitTesting;
using MPWeb.Logic.BLL;
using MPWeb.Logic.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MPWebBackEndUnitTests
{
    [TestClass]
    public class AESCryptoHelperTests
    {
        [TestMethod]
        public void TestAESCHSimpleInput()
        {
            var aCH = new AESCryptoHelper();
            var aesKey = "VhHe1F6DExaWl1T0bcOxdok58CyIXnjwCDQmojbwpH4=";
            var aesIV = "GFXXSSi7IQFN0bgbwuuVng==";

            var s0 = "HeLlOlLo { tEsTS tr 1 } trololo";
            var encS0 = aCH.EncryptString(s0, aesKey, aesIV);
            var decS0 = aCH.DecryptString(encS0, aesKey, aesIV);

            Assert.AreEqual(s0, decS0, "Encrypted and decrypted inputs are NOT the same.");
        }

        [TestMethod]
        public void TestAESCHComplexInput()
        {
            var aCH = new AESCryptoHelper();
            var aesKey = "VhHe1F6DExaWl1T0bcOxdok58CyIXnjwCDQmojbwpH4=";
            var aesIV = "GFXXSSi7IQFN0bgbwuuVng==";

            var tourPoints = new List<PMTracedTour>();
            var r = new Random();
            for (var i = 0; i < 50; i++)
            {
                tourPoints.Add(new PMTracedTour
                {
                    TourID = r.Next(),
                    Order = r.Next()
                });
            }

            var s0 = JsonConvert.SerializeObject(tourPoints);

            var encS0 = aCH.EncryptString(s0, aesKey, aesIV);
            var decS0 = aCH.DecryptString(encS0, aesKey, aesIV);

            Assert.AreEqual(s0, decS0, "Encrypted and decrypted inputs are NOT the same.");
        }

        
    }
}
