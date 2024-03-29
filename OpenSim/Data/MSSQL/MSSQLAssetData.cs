/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections.Generic;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;

namespace OpenSim.Data.MSSQL
{
    /// <summary>
    /// A MSSQL Interface for the Asset server
    /// </summary>
    public class MSSQLAssetData : AssetDataBase
    {
        private const string _migrationStore = "AssetStore";

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private long m_ticksToEpoch;
        /// <summary>
        /// Database manager
        /// </summary>
        private MSSQLManager m_database;

        #region IPlugin Members

        override public void Dispose() { }

        /// <summary>
        /// <para>Initialises asset interface</para>
        /// </summary>
        // [Obsolete("Cannot be default-initialized!")]
        override public void Initialise()
        {
            m_log.Info("[MSSQLUserData]: " + Name + " cannot be default-initialized!");
            throw new PluginNotInitialisedException(Name);
        }

        /// <summary>
        /// Initialises asset interface
        /// </summary>
        /// <para>
        /// a string instead of file, if someone writes the support
        /// </para>
        /// <param name="connectionString">connect string</param>
        override public void Initialise(string connectionString)
        {
            m_ticksToEpoch = new System.DateTime(1970, 1, 1).Ticks;

            if (!string.IsNullOrEmpty(connectionString))
            {
                m_database = new MSSQLManager(connectionString);
            }
            else
            {

                IniFile gridDataMSSqlFile = new IniFile("mssql_connection.ini");
                string settingDataSource = gridDataMSSqlFile.ParseFileReadValue("data_source");
                string settingInitialCatalog = gridDataMSSqlFile.ParseFileReadValue("initial_catalog");
                string settingPersistSecurityInfo = gridDataMSSqlFile.ParseFileReadValue("persist_security_info");
                string settingUserId = gridDataMSSqlFile.ParseFileReadValue("user_id");
                string settingPassword = gridDataMSSqlFile.ParseFileReadValue("password");

                m_database =
                    new MSSQLManager(settingDataSource, settingInitialCatalog, settingPersistSecurityInfo, settingUserId,
                                     settingPassword);
            }

            //New migration to check for DB changes
            m_database.CheckMigration(_migrationStore);
        }

        /// <summary>
        /// Database provider version.
        /// </summary>
        override public string Version
        {
            get { return m_database.getVersion(); }
        }

        /// <summary>
        /// The name of this DB provider.
        /// </summary>
        override public string Name
        {
            get { return "MSSQL Asset storage engine"; }
        }

        #endregion

        #region IAssetDataPlugin Members

        /// <summary>
        /// Fetch Asset from m_database
        /// </summary>
        /// <param name="assetID">the asset UUID</param>
        /// <returns></returns>
        override protected AssetBase FetchStoredAsset(UUID assetID)
        {
            string sql = "SELECT * FROM assets WHERE id = @id";
            using (AutoClosingSqlCommand command = m_database.Query(sql))
            {
                command.Parameters.Add(m_database.CreateParameter("id", assetID));
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        AssetBase asset = new AssetBase();
                        // Region Main
                        asset.FullID = new UUID((Guid)reader["id"]);
                        asset.Name = (string)reader["name"];
                        asset.Description = (string)reader["description"];
                        asset.Type = Convert.ToSByte(reader["assetType"]);
                        asset.Local = Convert.ToBoolean(reader["local"]);
                        asset.Temporary = Convert.ToBoolean(reader["temporary"]);
                        asset.Data = (byte[])reader["data"];
                        return asset;
                    }
                    return null; // throw new Exception("No rows to return");
                }
            }
        }

        /// <summary>
        /// Create asset in m_database
        /// </summary>
        /// <param name="asset">the asset</param>
        override public void CreateAsset(AssetBase asset)
        {
            if (ExistsAsset(asset.FullID))
            {
                return;
            }
            string sql = @"INSERT INTO assets
                            ([id], [name], [description], [assetType], [local], 
                             [temporary], [create_time], [access_time], [data])
                           VALUES
                            (@id, @name, @description, @assetType, @local, 
                             @temporary, @create_time, @access_time, @data)";
            using (AutoClosingSqlCommand command = m_database.Query(sql))
            {
                int now = (int)((System.DateTime.Now.Ticks - m_ticksToEpoch) / 10000000);
                command.Parameters.Add(m_database.CreateParameter("id", asset.FullID));
                command.Parameters.Add(m_database.CreateParameter("name", asset.Name));
                command.Parameters.Add(m_database.CreateParameter("description", asset.Description));
                command.Parameters.Add(m_database.CreateParameter("assetType", asset.Type));
                command.Parameters.Add(m_database.CreateParameter("local", asset.Local));
                command.Parameters.Add(m_database.CreateParameter("temporary", asset.Temporary));
                command.Parameters.Add(m_database.CreateParameter("access_time", now));
                command.Parameters.Add(m_database.CreateParameter("create_time", now));
                command.Parameters.Add(m_database.CreateParameter("data", asset.Data));

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Update asset in m_database
        /// </summary>
        /// <param name="asset">the asset</param>
        override public void UpdateAsset(AssetBase asset)
        {
            string sql = @"UPDATE assets set id = @id, name = @name, description = @description, assetType = @assetType,
                            local = @local, temporary = @temporary, data = @data
                           WHERE id = @keyId;";
            using (AutoClosingSqlCommand command = m_database.Query(sql))
            {
                command.Parameters.Add(m_database.CreateParameter("id", asset.FullID));
                command.Parameters.Add(m_database.CreateParameter("name", asset.Name));
                command.Parameters.Add(m_database.CreateParameter("description", asset.Description));
                command.Parameters.Add(m_database.CreateParameter("assetType", asset.Type));
                command.Parameters.Add(m_database.CreateParameter("local", asset.Local));
                command.Parameters.Add(m_database.CreateParameter("temporary", asset.Temporary));
                command.Parameters.Add(m_database.CreateParameter("data", asset.Data));
                command.Parameters.Add(m_database.CreateParameter("@keyId", asset.FullID));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    m_log.Error(e.ToString());
                }
            }
        }

// Commented out since currently unused - this probably should be called in FetchAsset()
//        private void UpdateAccessTime(AssetBase asset)
//        {
//            using (AutoClosingSqlCommand cmd = m_database.Query("UPDATE assets SET access_time = @access_time WHERE id=@id"))
//            {
//                int now = (int)((System.DateTime.Now.Ticks - m_ticksToEpoch) / 10000000);
//                cmd.Parameters.AddWithValue("@id", asset.FullID.ToString());
//                cmd.Parameters.AddWithValue("@access_time", now);
//                try
//                {
//                    cmd.ExecuteNonQuery();
//                }
//                catch (Exception e)
//                {
//                    m_log.Error(e.ToString());
//                }
//            }
//        }

        /// <summary>
        /// Check if asset exist in m_database
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>true if exist.</returns>
        override public bool ExistsAsset(UUID uuid)
        {
            if (FetchAsset(uuid) != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a list of AssetMetadata objects. The list is a subset of
        /// the entire data set offset by <paramref name="start" /> containing
        /// <paramref name="count" /> elements.
        /// </summary>
        /// <param name="start">The number of results to discard from the total data set.</param>
        /// <param name="count">The number of rows the returned list should contain.</param>
        /// <returns>A list of AssetMetadata objects.</returns>
        public override List<AssetMetadata> FetchAssetMetadataSet(int start, int count)
        {
            List<AssetMetadata> retList = new List<AssetMetadata>(count);
            string sql = @"SELECT (name,description,assetType,temporary,id), Row = ROW_NUMBER() 
                            OVER (ORDER BY (some column to order by)) 
                            WHERE Row >= @Start AND Row < @Start + @Count";
            using (AutoClosingSqlCommand command = m_database.Query(sql))
            {
                command.Parameters.Add(m_database.CreateParameter("start", start));
                command.Parameters.Add(m_database.CreateParameter("count", count));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AssetMetadata metadata = new AssetMetadata();
                        metadata.FullID = new UUID((Guid)reader["id"]);
                        metadata.Name = (string)reader["name"];
                        metadata.Description = (string)reader["description"];
                        metadata.Type = Convert.ToSByte(reader["assetType"]);
                        metadata.Temporary = Convert.ToBoolean(reader["temporary"]);
                    }
                }
            }

            return retList;
        }

        #endregion
    }
}
