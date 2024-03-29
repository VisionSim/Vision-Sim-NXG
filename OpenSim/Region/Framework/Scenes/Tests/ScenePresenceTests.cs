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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using Timer=System.Timers.Timer;
using Nini.Config;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Interregion;
using OpenSim.Region.CoreModules.World.Serialiser;
using OpenSim.Tests.Common;
using OpenSim.Tests.Common.Mock;
using OpenSim.Tests.Common.Setup;

namespace OpenSim.Region.Framework.Scenes.Tests
{
    /// <summary>
    /// Scene presence tests
    /// </summary>
    [TestFixture]
    public class ScenePresenceTests
    {
        public Scene scene, scene2, scene3;
        public UUID agent1, agent2, agent3;
        public static Random random;
        public ulong region1,region2,region3;
        public TestCommunicationsManager cm;
        public AgentCircuitData acd1;
        public SceneObjectGroup sog1, sog2, sog3;
        public TestClient testclient;

        [TestFixtureSetUp]
        public void Init()
        {
            cm = new TestCommunicationsManager();
            scene = SceneSetupHelpers.SetupScene("Neighbour x", UUID.Random(), 1000, 1000, cm);
            scene2 = SceneSetupHelpers.SetupScene("Neighbour x+1", UUID.Random(), 1001, 1000, cm);
            scene3 = SceneSetupHelpers.SetupScene("Neighbour x-1", UUID.Random(), 999, 1000, cm);

            IRegionModule interregionComms = new RESTInterregionComms();
            interregionComms.Initialise(scene, new IniConfigSource());
            interregionComms.Initialise(scene2, new IniConfigSource());
            interregionComms.Initialise(scene3, new IniConfigSource());
            interregionComms.PostInitialise();
            SceneSetupHelpers.SetupSceneModules(scene, new IniConfigSource(), interregionComms);
            SceneSetupHelpers.SetupSceneModules(scene2, new IniConfigSource(), interregionComms);
            SceneSetupHelpers.SetupSceneModules(scene3, new IniConfigSource(), interregionComms);

            agent1 = UUID.Random();
            agent2 = UUID.Random();
            agent3 = UUID.Random();
            random = new Random();
            sog1 = NewSOG(UUID.Random(), scene, agent1);
            sog2 = NewSOG(UUID.Random(), scene, agent1);
            sog3 = NewSOG(UUID.Random(), scene, agent1);

            //ulong neighbourHandle = Utils.UIntsToLong((uint)(neighbourx * Constants.RegionSize), (uint)(neighboury * Constants.RegionSize));
            region1 = scene.RegionInfo.RegionHandle;
            region2 = scene2.RegionInfo.RegionHandle;
            region3 = scene3.RegionInfo.RegionHandle;
        }

        /// <summary>
        /// Test adding a root agent to a scene.  Doesn't yet actually complete crossing the agent into the scene.
        /// </summary>
        [Test]
        public void T010_TestAddRootAgent()
        {
            TestHelper.InMethod();

            string firstName = "testfirstname";

            AgentCircuitData agent = new AgentCircuitData();
            agent.AgentID = agent1;
            agent.firstname = firstName;
            agent.lastname = "testlastname";
            agent.SessionID = UUID.Zero;
            agent.SecureSessionID = UUID.Zero;
            agent.circuitcode = 123;
            agent.BaseFolder = UUID.Zero;
            agent.InventoryFolder = UUID.Zero;
            agent.startpos = Vector3.Zero;
            agent.CapsPath = GetRandomCapsObjectPath();

            string reason;
            scene.NewUserConnection(agent, out reason);
            testclient = new TestClient(agent, scene);
            scene.AddNewClient(testclient);

            ScenePresence presence = scene.GetScenePresence(agent1);

            Assert.That(presence, Is.Not.Null, "presence is null");
            Assert.That(presence.Firstname, Is.EqualTo(firstName), "First name not same");
            acd1 = agent;
        }

        /// <summary>
        /// Test removing an uncrossed root agent from a scene.
        /// </summary>
        [Test]
        public void T011_TestRemoveRootAgent()
        {
            TestHelper.InMethod();

            scene.RemoveClient(agent1);

            ScenePresence presence = scene.GetScenePresence(agent1);

            Assert.That(presence, Is.Null, "presence is not null");
        }

        [Test]
        public void T012_TestAddNeighbourRegion()
        {
            TestHelper.InMethod();

            string reason;
            scene.NewUserConnection(acd1, out reason);
            scene.AddNewClient(testclient);

            ScenePresence presence = scene.GetScenePresence(agent1);
            presence.MakeRootAgent(new Vector3(90,90,90),false);

            string cap = presence.ControllingClient.RequestClientInfo().CapsPath;

            presence.AddNeighbourRegion(region2, cap);
            presence.AddNeighbourRegion(region3, cap);

            List<ulong> neighbours = presence.GetKnownRegionList();

            Assert.That(neighbours.Count, Is.EqualTo(2));
        }

        [Test]
        public void T013_TestRemoveNeighbourRegion()
        {
            TestHelper.InMethod();

            ScenePresence presence = scene.GetScenePresence(agent1);
            presence.RemoveNeighbourRegion(region3);

            List<ulong> neighbours = presence.GetKnownRegionList();
            Assert.That(neighbours.Count,Is.EqualTo(1));
            /*
            presence.MakeChildAgent;
            presence.MakeRootAgent;
            CompleteAvatarMovement
            */
        }

        [Test]
        public void T020_TestMakeRootAgent()
        {
            TestHelper.InMethod();

            ScenePresence presence = scene.GetScenePresence(agent1);
            Assert.That(presence.IsChildAgent, Is.False, "Starts out as a root agent");

            presence.MakeChildAgent();
            Assert.That(presence.IsChildAgent, Is.True, "Did not change to child agent after MakeChildAgent");

            // Accepts 0 but rejects Constants.RegionSize
            Vector3 pos = new Vector3(0,Constants.RegionSize-1,0);
            presence.MakeRootAgent(pos,true);
            Assert.That(presence.IsChildAgent, Is.False, "Did not go back to root agent");
            Assert.That(presence.AbsolutePosition, Is.EqualTo(pos), "Position is not the same one entered");
        }

        [Test]
        public void T021_TestCrossToNewRegion()
        {
            TestHelper.InMethod();

            // Adding child agent to region 1001
            string reason;
            scene2.NewUserConnection(acd1, out reason);
            scene2.AddNewClient(testclient);

            ScenePresence presence = scene.GetScenePresence(agent1);
            ScenePresence presence2 = scene2.GetScenePresence(agent1);

            // Adding neighbour region caps info to presence2
            string cap = presence.ControllingClient.RequestClientInfo().CapsPath;
            presence2.AddNeighbourRegion(region1, cap);

            scene.RegisterRegionWithGrid();
            scene2.RegisterRegionWithGrid();

            Assert.That(presence.IsChildAgent, Is.False, "Did not start root in origin region.");
            Assert.That(presence2.IsChildAgent, Is.True, "Is not a child on destination region.");

            // Cross to x+1
            presence.AbsolutePosition = new Vector3(Constants.RegionSize+1,3,100);
            presence.Update();

            EventWaitHandle wh = new EventWaitHandle (false, EventResetMode.AutoReset, "Crossing");

            // Mimicking communication between client and server, by waiting OK from client
            // sent by TestClient.CrossRegion call. Originally, this is network comm.
            if (!wh.WaitOne(5000,false))
            {
                presence.Update();
                if (!wh.WaitOne(8000,false))
                    throw new ArgumentException("1 - Timeout waiting for signal/variable.");
            }

            // This is a TestClient specific method that fires OnCompleteMovementToRegion event, which
            // would normally be fired after receiving the reply packet from comm. done on the last line.
            testclient.CompleteMovement();

            // Crossings are asynchronous
            int timer = 10;

            // Make sure cross hasn't already finished
            if (!presence.IsInTransit && !presence.IsChildAgent)
            {
                // If not and not in transit yet, give it some more time
                Thread.Sleep(5000);
            }

            // Enough time, should at least be in transit by now.
            while (presence.IsInTransit && timer > 0)
            {
                Thread.Sleep(1000);
                timer-=1;
            }

            Assert.That(timer,Is.GreaterThan(0),"Timed out waiting to cross 2->1.");
            Assert.That(presence.IsChildAgent, Is.True, "Did not complete region cross as expected.");
            Assert.That(presence2.IsChildAgent, Is.False, "Did not receive root status after receiving agent.");

            // Cross Back
            presence2.AbsolutePosition = new Vector3(-10, 3, 100);
            presence2.Update();

            if (!wh.WaitOne(5000,false))
            {
                presence2.Update();
                if (!wh.WaitOne(8000,false))
                    throw new ArgumentException("2 - Timeout waiting for signal/variable.");
            }
            testclient.CompleteMovement();

            if (!presence2.IsInTransit && !presence2.IsChildAgent)
            {
                // If not and not in transit yet, give it some more time
                Thread.Sleep(5000);
            }

            // Enough time, should at least be in transit by now.
            while (presence2.IsInTransit && timer > 0)
            {
                Thread.Sleep(1000);
                timer-=1;
            }

            Assert.That(timer,Is.GreaterThan(0),"Timed out waiting to cross 1->2.");
            Assert.That(presence2.IsChildAgent, Is.True, "Did not return from region as expected.");
            Assert.That(presence.IsChildAgent, Is.False, "Presence was not made root in old region again.");
        }

        [Test]
        public void T030_TestAddAttachments()
        {
            TestHelper.InMethod();

            ScenePresence presence = scene.GetScenePresence(agent1);

            presence.AddAttachment(sog1);
            presence.AddAttachment(sog2);
            presence.AddAttachment(sog3);

            Assert.That(presence.HasAttachments(), Is.True);
            Assert.That(presence.ValidateAttachments(), Is.True);
        }

        [Test]
        public void T031_RemoveAttachments()
        {
            TestHelper.InMethod();

            ScenePresence presence = scene.GetScenePresence(agent1);
            presence.RemoveAttachment(sog1);
            presence.RemoveAttachment(sog2);
            presence.RemoveAttachment(sog3);
            Assert.That(presence.HasAttachments(), Is.False);
        }

        [Test]
        public void T032_CrossAttachments()
        {
            TestHelper.InMethod();

            ScenePresence presence = scene.GetScenePresence(agent1);
            ScenePresence presence2 = scene2.GetScenePresence(agent1);
            presence2.AddAttachment(sog1);
            presence2.AddAttachment(sog2);

            IRegionModule serialiser = new SerialiserModule();
            SceneSetupHelpers.SetupSceneModules(scene, new IniConfigSource(), serialiser);
            SceneSetupHelpers.SetupSceneModules(scene2, new IniConfigSource(), serialiser);

            Assert.That(presence.HasAttachments(), Is.False, "Presence has attachments before cross");

            Assert.That(presence2.CrossAttachmentsIntoNewRegion(region1, true), Is.True, "Cross was not successful");
            Assert.That(presence2.HasAttachments(), Is.False, "Presence2 objects were not deleted");
            Assert.That(presence.HasAttachments(), Is.True, "Presence has not received new objects");
        }

        public static string GetRandomCapsObjectPath()
        {
            TestHelper.InMethod();

            UUID caps = UUID.Random();
            string capsPath = caps.ToString();
            capsPath = capsPath.Remove(capsPath.Length - 4, 4);
            return capsPath;
        }

        private SceneObjectGroup NewSOG(UUID uuid, Scene scene, UUID agent)
        {
            SceneObjectPart sop = new SceneObjectPart();
            sop.Name = RandomName();
            sop.Description = RandomName();
            sop.Text = RandomName();
            sop.SitName = RandomName();
            sop.TouchName = RandomName();
            sop.UUID = uuid;
            sop.Shape = PrimitiveBaseShape.Default;
            sop.Shape.State = 1;
            sop.OwnerID = agent;

            SceneObjectGroup sog = new SceneObjectGroup();
            sog.SetScene(scene);
            sog.SetRootPart(sop);

            return sog;
        }

        private static string RandomName()
        {
            StringBuilder name = new StringBuilder();
            int size = random.Next(5,12);
            char ch ;
            for (int i=0; i<size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65))) ;
                name.Append(ch);
            }
            return name.ToString();
        }
    }
}
