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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class UserServicesConnector : IUserDataService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = String.Empty;

        public UserServicesConnector()
        {
        }

        public UserServicesConnector(string serverURI)
        {
            m_ServerURI = serverURI.TrimEnd('/');
        }

        public UserServicesConnector(IConfigSource source)
        {
            Initialise(source);
        }

        public virtual void Initialise(IConfigSource source)
        {
            IConfig assetConfig = source.Configs["UserService"];
            if (assetConfig == null)
            {
                m_log.Error("[USER CONNECTOR]: UserService missing from OpanSim.ini");
                throw new Exception("User connector init error");
            }

            string serviceURI = assetConfig.GetString("UserServerURI",
                    String.Empty);

            if (serviceURI == String.Empty)
            {
                m_log.Error("[USER CONNECTOR]: No Server URI named in section UserService");
                throw new Exception("User connector init error");
            }
            m_ServerURI = serviceURI;
        }

        public UserData GetUserData(UUID scopeID, string firstName, string lastName)
        {
            string uri = m_ServerURI + "/users/";
            UserData data = new UserData();
            data.FirstName = firstName;
            data.LastName = lastName;
            data.ScopeID = scopeID;
            data.UserID = UUID.Zero;

            try
            {
                data = SynchronousRestObjectRequester.
                        MakeRequest<UserData, UserData>("POST", uri, data);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[USER CONNECTOR]: Unable to send request to user server. Reason: {1}", e.Message);
                return null;
            }

            if (data.UserID == UUID.Zero)
                return null;

            return data;
        }

        public UserData GetUserData(UUID scopeID, UUID userID)
        {
            string uri = m_ServerURI + "/users/";
            UserData data = new UserData();
            data.FirstName = String.Empty;
            data.LastName = String.Empty;
            data.ScopeID = scopeID;
            data.UserID = userID;

            try
            {
                data = SynchronousRestObjectRequester.
                        MakeRequest<UserData, UserData>("POST", uri, data);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[USER CONNECTOR]: Unable to send request to user server. Reason: {1}", e.Message);
                return null;
            }

            if (data.UserID == UUID.Zero)
                return null;

            return data;
        }

        public bool SetUserData(UserData data)
        {
            string uri = m_ServerURI + "/user/";
            bool result = false;

            try
            {
                result = SynchronousRestObjectRequester.
                        MakeRequest<UserData, bool>("POST", uri, data);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[USER CONNECTOR]: Unable to send request to user server. Reason: {1}", e.Message);
                return false;
            }

            return result;
        }

        public List<UserData> GetAvatarPickerData(UUID scopeID, string query)
        {
            string uri = m_ServerURI + "/userlist/";
            UserData data = new UserData();
            data.FirstName = query;
            data.ScopeID = scopeID;
            List<UserData> result;

            try
            {
                result = SynchronousRestObjectRequester.
                        MakeRequest<UserData, List<UserData>>("POST", uri, data);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[USER CONNECTOR]: Unable to send request to user server. Reason: {1}", e.Message);
                return null;
            }

            return result;
        }
    }
}
