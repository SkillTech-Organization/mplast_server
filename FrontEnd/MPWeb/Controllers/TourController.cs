using MPWeb.Caching;
using MPWeb.Code;
using MPWeb.Logic.BLL;
using MPWeb.Logic.BLL.TrackingEngine;
using MPWeb.Logic.Cache;
using MPWeb.Logic.Tables;
using MPWeb.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MPWeb.Controllers
{
    public class TourController : CustomControllerBase
    {
        [HttpGet]
        [AuthorizeByRole("GetTourData")]
        [Route("Tour/GetTourData")]
        public ActionResult GetTourData(DateTime filterDate)
        {
            if (filterDate == null)
            {
                // TODO return error
            }

            var bllWTT = new BllWebTraceTour(Environment.MachineName);
            var userCtx = (ServerUserContext)Session["ServerUserContext"];
            if (userCtx.Roles.Contains("TemporaryUser"))
            {
                /*
                int shiftHours = 0;
                t.Start = new DateTime(t.Start.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
                */
                return nsJson(bllWTT.RetrieveCachedListForTempUser(userCtx.TourPointList));
            }
            else
            {
                return nsJson(bllWTT.RetrieveProcessedCachedList(filterDate.ToUniversalTime()));
            }
        }

        [HttpGet]
        [AuthorizeByRole("GetVehicleData")]
        [Route("Tour/GetTourCompletionData")]
        public ActionResult GetTourCompletionData()
        {
            var bllWTT = new BllWebTraceTour(Environment.MachineName);
            var userCtx = (ServerUserContext)Session["ServerUserContext"];
            if (userCtx.Roles.Contains("TemporaryUser"))
            {
                return nsJson(bllWTT.RetrieveCachedCompletionList(userCtx.TourPointList));
            }
            else
            {
                return nsJson(bllWTT.RetrieveCachedCompletionList());
            }
        }

        [HttpGet]
   //     [AuthorizeByRole("GetVehicleData")]
        [Route("Tour/GetVehicleData")]
        public ActionResult GetVehicleData(DateTime filterDate)
        {
            var bllWVT = new BllWebVehicleTrace();

            var userCtx = (ServerUserContext)Session["ServerUserContext"];
            if (userCtx.Roles.Contains("TemporaryUser"))
            {
                return nsJson(bllWVT.RetriveCachedFilteredVehicleDataForTempUser(userCtx.TourPointList));
            }
            else
            {
                return nsJson(bllWVT.RetriveCachedVehicleData(filterDate.ToUniversalTime()));
            }
        }

        [HttpGet]
        [AuthorizeByRole("RegisteredUser")]
        [Route("Tour/ReinitializeTourTracking")]
        public ActionResult ReinitializeTourTracking()
        {
            try
            {
                var bllWVT = new BllWebVehicleTrace();

                var userCtx = (ServerUserContext)Session["ServerUserContext"];
                if (userCtx.Roles.Contains("TemporaryUser"))
                {
                    return new HttpUnauthorizedResult();
                }

                VehicleTrackingDataCaching.Instance.StopThread();

                BllWebTraceTour bllWebTraceTour = new BllWebTraceTour(userCtx.Name);
                int Total;
                var retRawX = bllWebTraceTour.RetrieveList(out Total);
                ToursCache.Instance.Items = new System.Collections.Concurrent.ConcurrentBag<PMTour>(retRawX);

                TrackingEngine.Instance.InitTrackingEngine();
                VehicleTrackingDataCaching.Instance.RestartThread();

                return nsJson(new { reset = "OK" });
            }
            catch (Exception ex)
            {
                throw new AppException(ViewBag.CorrelationId, ex);
            }
        }

        [HttpGet]
        [AuthorizeByRole("ADMIN")]
        [Route("Tour/ExportUserTrackingCSV")]
        public ActionResult ExportUserTrackingCSV(DateTime filterFrom, DateTime filterTo)
        {
            try
            {
                var bllPML = new BllPMLogin(Environment.MachineName);

                var userCtx = (ServerUserContext)Session["ServerUserContext"];
                if (userCtx.Roles.Contains("TemporaryUser"))
                {
                    return new HttpUnauthorizedResult();
                }


                filterFrom = new DateTime(filterFrom.Year, filterFrom.Month, filterFrom.Day);
                filterTo = new DateTime(filterTo.Year, filterTo.Month, filterTo.Day);
                filterTo = filterTo.Date.AddMinutes(1439);
                var loginCSV = bllPML.GetLoginsCSV(filterFrom.ToUniversalTime().Ticks, filterTo.ToUniversalTime().Ticks);

                return nsJson(new
                {
                    loginCSV = loginCSV
                });
            }
            catch (Exception ex)
            {
                throw new AppException(ViewBag.CorrelationId, ex);
            }
        }

        [HttpPost]
        [AuthorizeByRole("ADMIN")]
        [Route("Tour/SetManualTrackingData")]
        public ActionResult SetManualTrackingData(DateTime TourStartLocalTime, DateTime TimestampLocalTime, string Device, double Latitude, double Longitude)
        {
            // TODO write to persistent storage that the user has logged in

            try
            {
                TrackingEngine.Instance.SetManualTrackingData(TourStartLocalTime, TimestampLocalTime, Device, Latitude, Longitude);

                return nsJson(new { SetManualTrackingData = "OK" });
            }
            catch (Exception ex)
            {
                throw new AppException(ex.Message, ex.InnerException);
            }
        }
    }
}