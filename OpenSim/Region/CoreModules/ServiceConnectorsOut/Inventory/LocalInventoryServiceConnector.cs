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

using log4net;
using Nini.Config;

using System;
using System.Collections.Generic;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Data;
using OpenSim.Server.Base;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenMetaverse;


namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory
{
    public class LocalInventoryServicesConnector : ISharedRegionModule, IInventoryService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IInventoryService m_InventoryService;

        private bool m_Enabled = false;
        private bool m_Initialized = false;

        public string Name
        {
            get { return "LocalInventoryServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("InventoryServices", "");
                if (name == Name)
                {
                    IConfig inventoryConfig = source.Configs["InventoryService"];
                    if (inventoryConfig == null)
                    {
                        m_log.Error("[INVENTORY CONNECTOR]: InventoryService missing from OpenSim.ini");
                        return;
                    }

                    string serviceDll = inventoryConfig.GetString("LocalServiceModule", String.Empty);

                    if (serviceDll == String.Empty)
                    {
                        m_log.Error("[INVENTORY CONNECTOR]: No LocalServiceModule named in section InventoryService");
                        return;
                    }

                    Object[] args = new Object[] { source };
                    m_log.DebugFormat("[INVENTORY CONNECTOR]: Service dll = {0}", serviceDll);

                    m_InventoryService = ServerUtils.LoadPlugin<IInventoryService>(serviceDll, args);

                    if (m_InventoryService == null)
                    {
                        m_log.Error("[INVENTORY CONNECTOR]: Can't load inventory service");
                        //return;
                        throw new Exception("Unable to proceed. Please make sure your ini files in config-include are updated according to .example's");
                    }

                    //List<IInventoryDataPlugin> plugins
                    //    = DataPluginFactory.LoadDataPlugins<IInventoryDataPlugin>(
                    //        configSettings.StandaloneInventoryPlugin,
                    //        configSettings.StandaloneInventorySource);

                    //foreach (IInventoryDataPlugin plugin in plugins)
                    //{
                    //    // Using the OSP wrapper plugin for database plugins should be made configurable at some point
                    //    m_InventoryService.AddPlugin(new OspInventoryWrapperPlugin(plugin, this));
                    //}

                    m_Enabled = true;
                    m_log.Info("[INVENTORY CONNECTOR]: Local inventory connector enabled");
                }
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (!m_Initialized)
            {
                // ugh!
                scene.CommsManager.UserProfileCacheService.SetInventoryService(this);
                scene.CommsManager.UserService.SetInventoryService(this);
                m_Initialized = true;
            }

//            m_log.DebugFormat(
//                "[INVENTORY CONNECTOR]: Registering IInventoryService to scene {0}", scene.RegionInfo.RegionName);
            
            scene.RegisterModuleInterface<IInventoryService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_log.InfoFormat(
                "[INVENTORY CONNECTOR]: Enabled local invnetory for region {0}", scene.RegionInfo.RegionName);
        }

        #region IInventoryService

        public bool CreateUserInventory(UUID user)
        {
            return m_InventoryService.CreateUserInventory(user);
        }

        public List<InventoryFolderBase> GetInventorySkeleton(UUID userId)
        {
            return m_InventoryService.GetInventorySkeleton(userId);
        }

        public InventoryCollection GetUserInventory(UUID id)
        {
            return m_InventoryService.GetUserInventory(id);
        }

        public void GetUserInventory(UUID userID, InventoryReceiptCallback callback)
        {
            m_InventoryService.GetUserInventory(userID, callback);
        }

        public List<InventoryItemBase> GetFolderItems(UUID userID, UUID folderID)
        {
            return m_InventoryService.GetFolderItems(userID, folderID);
        }

        /// <summary>
        /// Add a new folder to the user's inventory
        /// </summary>
        /// <param name="folder"></param>
        /// <returns>true if the folder was successfully added</returns>
        public bool AddFolder(InventoryFolderBase folder)
        {
            return m_InventoryService.AddFolder(folder);
        }

        /// <summary>
        /// Update a folder in the user's inventory
        /// </summary>
        /// <param name="folder"></param>
        /// <returns>true if the folder was successfully updated</returns>
        public bool UpdateFolder(InventoryFolderBase folder)
        {
            return m_InventoryService.UpdateFolder(folder);
        }

        /// <summary>
        /// Move an inventory folder to a new location
        /// </summary>
        /// <param name="folder">A folder containing the details of the new location</param>
        /// <returns>true if the folder was successfully moved</returns>
        public bool MoveFolder(InventoryFolderBase folder)
        {
            return m_InventoryService.MoveFolder(folder);
        }

        /// <summary>
        /// Purge an inventory folder of all its items and subfolders.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns>true if the folder was successfully purged</returns>
        public bool PurgeFolder(InventoryFolderBase folder)
        {
            return m_InventoryService.PurgeFolder(folder);
        }

        /// <summary>
        /// Add a new item to the user's inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the item was successfully added</returns>
        public bool AddItem(InventoryItemBase item)
        {
            return m_InventoryService.AddItem(item);
        }

        /// <summary>
        /// Update an item in the user's inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the item was successfully updated</returns>
        public bool UpdateItem(InventoryItemBase item)
        {
            return m_InventoryService.UpdateItem(item);
        }

        /// <summary>
        /// Delete an item from the user's inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the item was successfully deleted</returns>
        public bool DeleteItem(InventoryItemBase item)
        {
            return m_InventoryService.DeleteItem(item);
        }

        public InventoryItemBase QueryItem(InventoryItemBase item)
        {
            return m_InventoryService.QueryItem(item);
        }

        public InventoryFolderBase QueryFolder(InventoryFolderBase folder)
        {
            return m_InventoryService.QueryFolder(folder);
        }

        /// <summary>
        /// Does the given user have an inventory structure?
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool HasInventoryForUser(UUID userID)
        {
            return m_InventoryService.HasInventoryForUser(userID);
        }

        /// <summary>
        /// Retrieve the root inventory folder for the given user.
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>null if no root folder was found</returns>
        public InventoryFolderBase RequestRootFolder(UUID userID)
        {
            return m_InventoryService.RequestRootFolder(userID);
        }

        public List<InventoryItemBase> GetActiveGestures(UUID userId)
        {
            return m_InventoryService.GetActiveGestures(userId);
        }
        #endregion IInventoryService
    }
}
