﻿#region License

// Copyright (c) 2013 The Sentry Team and individual contributors.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
// 
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
// 
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
// 
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;

using NUnit.Framework;

using SharpRaven.Data;
using SharpRaven.UnitTests.Utilities;

namespace SharpRaven.UnitTests.Data
{
    [TestFixture]
    public class JsonPacketTests
    {
        private static void SimulateHttpRequest(Action<JsonPacket> test)
        {
            using (var simulator = new HttpSimulator())
            {
                using (simulator.SimulateRequest())
                {
                    var json = new JsonPacket(Guid.NewGuid().ToString("n"));
                    test.Invoke(json);
                }
            }
        }


        [Test]
        public void Constructor_NullException_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new JsonPacket(String.Empty, null));
            Assert.That(exception.ParamName, Is.EqualTo("exception"));
        }


        [Test]
        public void Constructor_NullProjectAndException_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new JsonPacket(null, null));
            Assert.That(exception.ParamName, Is.EqualTo("project"));
        }


        [Test]
        public void Constructor_NullProject_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new JsonPacket(null));
            Assert.That(exception.ParamName, Is.EqualTo("project"));
        }


        [Test]
        public void Constructor_ProjectAndException_EventIDIsValidGuid()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project, new Exception("Error"));

            Assert.That(json.EventID, Is.Not.Null.Or.Empty, "EventID");
            Assert.That(Guid.Parse(json.EventID), Is.Not.Null);
        }


        [Test]
        public void Constructor_ProjectAndException_MessageEqualsExceptionMessage()
        {
            var project = Guid.NewGuid().ToString();
            Exception exception = new Exception("Error");
            var json = new JsonPacket(project, exception);

            Assert.That(json.Message, Is.EqualTo(exception.Message));
        }


        [Test]
        public void Constructor_ProjectAndException_ModulesHasCountGreaterThanZero()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project, new Exception("Error"));

            Assert.That(json.Modules, Has.Count.GreaterThan(0));
        }


        [Test]
        public void Constructor_ProjectAndException_ProjectIsEqual()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project, new Exception("Error"));

            Assert.That(json.Project, Is.EqualTo(project));
        }


        [Test]
        public void Constructor_ProjectAndException_ServerNameEqualsMachineName()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project, new Exception("Error"));

            Assert.That(json.ServerName, Is.EqualTo(Environment.MachineName));
        }


        [Test]
        public void Constructor_Project_EventIDIsValidGuid()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project);

            Assert.That(json.EventID, Is.Not.Null.Or.Empty, "EventID");
            Assert.That(Guid.Parse(json.EventID), Is.Not.Null);
        }


        [Test]
        public void Constructor_Project_ModulesHasCountGreaterThanZero()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project);

            Assert.That(json.Modules, Has.Count.GreaterThan(0));
        }


        [Test]
        public void Constructor_Project_ProjectIsEqual()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project);

            Assert.That(json.Project, Is.EqualTo(project));
        }


        [Test]
        public void Constructor_Project_ServerNameEqualsMachineName()
        {
            var project = Guid.NewGuid().ToString();
            var json = new JsonPacket(project);

            Assert.That(json.ServerName, Is.EqualTo(Environment.MachineName));
        }


        [Test]
        [Category("NoMono")]
        public void Constructor_WithHttpContext_RequestIsNotNull()
        {
            SimulateHttpRequest(json => Assert.That(json.Request, Is.Not.Null));
        }


        [Test]
        [Category("NoMono")]
        public void Constructor_WithHttpContext_UsertIsNotNull()
        {
            SimulateHttpRequest(json =>
            {
                Assert.That(json.User, Is.Not.Null);
                Assert.That(json.User.IpAddress, Is.EqualTo("127.0.0.1"));
            });
        }
    }
}