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

using System.Reflection;
using log4net;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Communications.OGS1;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Communications.Hypergrid
{
    public class HGCommunicationsGridMode : CommunicationsManager // CommunicationsOGS1
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        IHyperlink m_osw = null;
        public IHyperlink HGServices
        {
            get { return m_osw; }
        }

        public HGCommunicationsGridMode(
            NetworkServersInfo serversInfo, BaseHttpServer httpServer,
            IAssetCache assetCache, SceneManager sman, LibraryRootFolder libraryRootFolder)
            : base(serversInfo, httpServer, assetCache, false, libraryRootFolder)
        {
            // From constructor at CommunicationsOGS1
            HGGridServices gridInterComms = new HGGridServicesGridMode(serversInfo, httpServer, assetCache, sman, m_userProfileCacheService);
            m_gridService = gridInterComms;
            m_osw = gridInterComms;

            // The HG InventoryService always uses secure handlers
            HGInventoryServiceClient invService = new HGInventoryServiceClient(serversInfo.InventoryURL, this.m_userProfileCacheService, true);
            invService.UserProfileCache = m_userProfileCacheService; 
            AddSecureInventoryService(invService);
            m_defaultInventoryHost = invService.Host;
            if (SecureInventoryService != null)
                m_log.Info("[HG]: SecureInventoryService.");
            else
                m_log.Info("[HG]: Non-secureInventoryService.");

            HGUserServices userServices = new HGUserServices(this);
            // This plugin arrangement could eventually be configurable rather than hardcoded here.
            userServices.AddPlugin(new TemporaryUserProfilePlugin());
            userServices.AddPlugin(new OGS1UserDataPlugin(this));            
            
            m_userService = userServices;
            m_messageService = userServices;
            m_avatarService = userServices;           
        }
    }
}
