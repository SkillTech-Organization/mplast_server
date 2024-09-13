using MPWeb.Logic.BLL.TrackingEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;

namespace MPWeb.Logic.BLL
{
    public class DBManager
    {
        private bool m_VehicleTrackingCacheDBPathModeRelative;
        private string m_VehicleTrackingCacheDBPath;
        private SQLiteConnection m_DBConnection = null;

        public SQLiteConnection Connection {
            get
            {
                return m_DBConnection;
            }
        }

        public DBManager(string databasePath = null)
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["VehicleTrackingCacheDBPathModeRelative"]))
            {
                throw new Exception("ERROR: Application parameter VehicleTrackingCacheDBPathModeRelative is not set.");
            }
            m_VehicleTrackingCacheDBPathModeRelative = bool.Parse(
                ConfigurationManager.AppSettings["VehicleTrackingCacheDBPathModeRelative"]);

            if (string.IsNullOrEmpty(databasePath))
            {
                if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["VehicleTrackingCacheDBPath"]))
                {
                    throw new Exception("Parameter VehicleTrackingCacheDBPath is not set.");
                }
                else if (m_VehicleTrackingCacheDBPathModeRelative)
                {
                    m_VehicleTrackingCacheDBPath = System.Web.Hosting.HostingEnvironment.MapPath(
                    "~" + ConfigurationManager.AppSettings["VehicleTrackingCacheDBPath"]);
                }
                else
                {
                    m_VehicleTrackingCacheDBPath = ConfigurationManager.AppSettings["VehicleTrackingCacheDBPath"];
                }
            }
            else
            {
                m_VehicleTrackingCacheDBPath = databasePath;
            }

            if (!File.Exists(m_VehicleTrackingCacheDBPath))
            {
                throw new Exception("ERROR: File given by VehicleTrackingCacheDBPath parameter does not exist: " +
                    m_VehicleTrackingCacheDBPath);
            }

            try
            {
                m_DBConnection = new SQLiteConnection("Data Source=" + m_VehicleTrackingCacheDBPath
                    + ";Version=3;New=False;Compress=True;");
                m_DBConnection.Open();
            }
            catch (Exception e)
            {
                throw new Exception("DBManager was unable to open database with path: " + m_VehicleTrackingCacheDBPath
                    + ". Exception: " + e.ToString());
            }
        }

        public void StoreVehicleTrackingDataRecord(DateTime timestamp, List<VehiclePositionData> trackingData)
        {
            if (trackingData == null || trackingData.Count == 0)
                return;
            var sqlCmd = m_DBConnection.CreateCommand();
            sqlCmd.CommandText = "INSERT INTO VehicleTrackingData(Timestamp, TrackingData) VALUES (?, ?)";
            sqlCmd.Parameters.Add(new SQLiteParameter("Timestamp", timestamp.Ticks));
            sqlCmd.Parameters.Add(new SQLiteParameter("TrackingData", JsonConvert.SerializeObject(trackingData)));

            try
            {
                sqlCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR: failed to store VehicleTrackingData row to SQLite3 database: " + ex.ToString());
            }
        }

        public void StoreVehicleTrackingDataRecord_manual(DateTime timestampUtc, VehiclePositionData trackingData)
        {
            if (trackingData == null)
                return;
            List<VehiclePositionData> lstVehiclePositionData = new List<VehiclePositionData>();
            lstVehiclePositionData.Add(trackingData);
            DateTime TourStart = trackingData.TourStart;
            string Device = trackingData.Device;
            double Latitude = trackingData.Latitude;
            double Longitude = trackingData.Longitude;

            var sqlCmdDel = m_DBConnection.CreateCommand();
            sqlCmdDel.CommandText = "DELETE FROM VehicleTrackingData_manual where Timestamp>=? and Device = ? and Latitude = ? and Longitude = ?";
            sqlCmdDel.Parameters.Add(new SQLiteParameter("Timestamp", TourStart.Ticks));
            sqlCmdDel.Parameters.Add(new SQLiteParameter("Device", Device));
            sqlCmdDel.Parameters.Add(new SQLiteParameter("Latitude", Latitude));
            sqlCmdDel.Parameters.Add(new SQLiteParameter("Longitude", Longitude));

            var sqlCmdIns = m_DBConnection.CreateCommand();
            sqlCmdIns.CommandText = "INSERT INTO VehicleTrackingData_manual(Timestamp, TrackingData, Device, Latitude, Longitude) VALUES (?, ?, ?, ?, ?)";
            sqlCmdIns.Parameters.Add(new SQLiteParameter("Timestamp", timestampUtc.Ticks));
            sqlCmdIns.Parameters.Add(new SQLiteParameter("TrackingData", JsonConvert.SerializeObject(lstVehiclePositionData)));
            sqlCmdIns.Parameters.Add(new SQLiteParameter("Device", Device));
            sqlCmdIns.Parameters.Add(new SQLiteParameter("Latitude", Latitude));
            sqlCmdIns.Parameters.Add(new SQLiteParameter("Longitude", Longitude));

            try
            {
                sqlCmdDel.ExecuteNonQuery();
                sqlCmdIns.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR: failed to store VehicleTrackingData row to SQLite3 database: " + ex.ToString());
            }
        }

        public void DeleteVehicleTrackingDataRecord(DateTime timestamp)
        {
            var sqlCmd = m_DBConnection.CreateCommand();
            sqlCmd.CommandText = "delete from VehicleTrackingData WHERE Timestamp <= ? ";
            sqlCmd.Parameters.Add(new SQLiteParameter("Timestamp", timestamp.ToUniversalTime().Ticks));

            var sqlCmd2 = m_DBConnection.CreateCommand();
            sqlCmd2.CommandText = "delete from VehicleTrackingData_manual WHERE Timestamp <= ? ";
            sqlCmd2.Parameters.Add(new SQLiteParameter("Timestamp", timestamp.ToUniversalTime().Ticks));


            var sqlCmd3 = m_DBConnection.CreateCommand();
            sqlCmd3.CommandText = "vacuum ";

            try
            {
                sqlCmd.ExecuteNonQuery();
                sqlCmd2.ExecuteNonQuery();
                sqlCmd3.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR: failed to delete VehicleTrackingData row to SQLite3 database: " + ex.ToString());
            }
        }


        public List<VehicleTrackingRecord> LoadVehicleTrackingRecordList(DateTime from, DateTime to)
        {
            var vtrList = new List<VehicleTrackingRecord>();

            var sqlCmd = m_DBConnection.CreateCommand();
            sqlCmd.CommandText = "select Timestamp, TrackingData from ( " + Environment.NewLine +
                                 "SELECT Timestamp, TrackingData FROM VehicleTrackingData WHERE Timestamp >= ? AND Timestamp <= ? " + Environment.NewLine +
                                 "UNION " + Environment.NewLine +
                                 "SELECT Timestamp, TrackingData FROM VehicleTrackingData_manual WHERE Timestamp >= ? AND Timestamp <= ? " + Environment.NewLine +
                                 ") order by Timestamp ";

            sqlCmd.Parameters.Add(new SQLiteParameter("TimestampFrom", from.Ticks));
            sqlCmd.Parameters.Add(new SQLiteParameter("TimestampTo", to.Ticks));
            sqlCmd.Parameters.Add(new SQLiteParameter("TimestampFrom_manual", from.Ticks));
            sqlCmd.Parameters.Add(new SQLiteParameter("TimestampTo_manual", to.Ticks));

            try
            {
                var reader = sqlCmd.ExecuteReader();
                if (reader.HasRows) {
                    while (reader.Read())
                    {
                        var rowOffset = 0;
                        var timeStamp = reader.GetInt64(rowOffset++);
                        var trackingDataJSON = reader.GetString(rowOffset++);
                        vtrList.Add(new VehicleTrackingRecord
                        {
                            Timestamp = new DateTime(timeStamp, DateTimeKind.Utc),
                            TrackingData = JsonConvert.DeserializeObject<List<VehiclePositionData>>(trackingDataJSON)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR: failed to loat VehicleTrackingData from SQLite3 database: " + ex.ToString());
            }

            return vtrList;
        }
    }
}
