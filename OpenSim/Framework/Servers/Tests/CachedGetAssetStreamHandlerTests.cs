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
using System.Text;
using NUnit.Framework;
using OpenSim.Data;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Tests.Common;
using OpenSim.Tests.Common.Mock;
using OpenSim.Tests.Common.Setup;

namespace OpenSim.Framework.Servers.Tests
{
    [TestFixture]
    public class CachedGetAssetStreamHandlerTests
    {
        private const string ASSETS_PATH = "/assets";

        [Test]
        public void TestConstructor()
        {
            TestHelper.InMethod();

            // CachedGetAssetStreamHandler handler = 
            new CachedGetAssetStreamHandler(null);
        }

        [Test]
        public void TestGetParams()
        {
            TestHelper.InMethod();

            CachedGetAssetStreamHandler handler = new CachedGetAssetStreamHandler(null);
            BaseRequestHandlerTestHelper.BaseTestGetParams(handler, ASSETS_PATH);
        }

        [Test]
        public void TestSplitParams()
        {
            TestHelper.InMethod();

            CachedGetAssetStreamHandler handler = new CachedGetAssetStreamHandler(null);
            BaseRequestHandlerTestHelper.BaseTestSplitParams(handler, ASSETS_PATH);
        }

        [Test]
        public void TestHandleNoParams()
        {
            TestHelper.InMethod();

            CachedGetAssetStreamHandler handler = new CachedGetAssetStreamHandler(null);

            BaseRequestHandlerTestHelper.BaseTestHandleNoParams(handler, ASSETS_PATH);
        }

        [Test]
        public void TestHandleMalformedGuid()
        {
            TestHelper.InMethod();

            CachedGetAssetStreamHandler handler = new CachedGetAssetStreamHandler(null);

            BaseRequestHandlerTestHelper.BaseTestHandleMalformedGuid(handler, ASSETS_PATH);
        }

        [Test]
        public void TestHandleFetchMissingAsset()
        {
            IAssetCache assetCache = new TestAssetCache();
            CachedGetAssetStreamHandler handler;
            OSHttpResponse response;
            AssetBase asset = CreateTestEnvironment(out handler, out response);

            GetAssetStreamHandlerTestHelpers.BaseFetchMissingAsset(handler, response);
        }

        [Test]
        public void TestHandleFetchExistingAssetData()
        {
            CachedGetAssetStreamHandler handler;
            OSHttpResponse response;
            AssetBase asset = CreateTestEnvironment(out handler, out response);

            GetAssetStreamHandlerTestHelpers.BaseFetchExistingAssetDataTest(asset, handler, response);
        }

        //[Test]
        //public void TestHandleFetchExistingAssetMetaData()
        //{
        //    CachedGetAssetStreamHandler handler;
        //    OSHttpResponse response;
        //    AssetBase asset = CreateTestEnvironment(out handler, out response);

        //    GetAssetStreamHandlerTestHelpers.BaseFetchExistingAssetMetaDataTest(asset, handler, response);
        //}

        private static AssetBase CreateTestEnvironment(out CachedGetAssetStreamHandler handler, out OSHttpResponse response)
        {
            AssetBase asset = GetAssetStreamHandlerTestHelpers.CreateCommonTestResources(out response);

            IAssetCache assetDataPlugin = new TestAssetCache();
            handler = new CachedGetAssetStreamHandler(assetDataPlugin);

            assetDataPlugin.AddAsset(asset);
            return asset;
        }
    }
}
