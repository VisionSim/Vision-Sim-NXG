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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;

using log4net;

namespace OpenSim.Region.OptionalModules.Scripting.Minimodule
{
    class SPAvatar : System.MarshalByRefObject, IAvatar
    {
        private readonly Scene m_rootScene;
        private readonly UUID m_ID;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SPAvatar(Scene scene, UUID ID)
        {
            m_rootScene = scene;
            m_ID = ID;
        }

        private ScenePresence GetSP()
        {
            return m_rootScene.GetScenePresence(m_ID);
        }

        public string Name
        {
            get { return GetSP().Name; }
            set { throw new InvalidOperationException("Avatar Names are a read-only property."); }
        }

        public UUID GlobalID
        {
            get { return m_ID; }
        }

        public Vector3 WorldPosition
        {
            get { return GetSP().AbsolutePosition; }
            set { GetSP().TeleportWithMomentum(value); }
        }
		
		#region IAvatar implementation
		public IAvatarAttachment[] Attachments
		{
			get {
				List<IAvatarAttachment> attachments = new List<IAvatarAttachment>();
				
				Hashtable internalAttachments = GetSP().Appearance.GetAttachments();
				if(internalAttachments != null)
				{
					foreach(DictionaryEntry element in internalAttachments)
					{
						Hashtable attachInfo = (Hashtable)element.Value;
						attachments.Add(new SPAvatarAttachment(m_rootScene, this, (int)element.Key, new UUID((string)attachInfo["item"]), new UUID((string)attachInfo["asset"])));
					}
				}
				
				return attachments.ToArray();
			}
		}

		public void LoadUrl(IObject sender, string message, string url)
		{
			IDialogModule dm = m_rootScene.RequestModuleInterface<IDialogModule>();
			if(dm != null)
				dm.SendUrlToUser(GetSP().UUID, sender.Name, sender.GlobalID, GetSP().UUID, false, message, url);
		}
		#endregion
    }
}
