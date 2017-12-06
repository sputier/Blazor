﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Blazor.E2ETest.Infrastructure
{
    public abstract class ServerFixture : IDisposable
    {
        public bool IsStarted => RootUri != null;
        public Uri RootUri { get; private set; }

        private IWebHost _host;

        public void Dispose()
        {
            _host.StopAsync();
        }

        protected void Start(IWebHost host)
        {
            if (_host != null)
            {
                throw new InvalidOperationException("Server is already started.");
            }

            _host = host ?? throw new ArgumentNullException(nameof(host));
            RootUri = new Uri(StartWebHostInBackgroundThread());
        }

        protected static string FindSolutionDir()
        {
            return FindClosestDirectoryContaining(
                "Blazor.sln",
                Path.GetDirectoryName(typeof(ServerFixture).Assembly.Location));
        }

        private static string FindClosestDirectoryContaining(
            string filename,
            string startDirectory)
        {
            var dir = startDirectory;
            while (true)
            {
                if (File.Exists(Path.Combine(dir, filename)))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
                if (string.IsNullOrEmpty(dir))
                {
                    throw new FileNotFoundException(
                        $"Could not locate a file called '{filename}' in " +
                        $"directory '{startDirectory}' or any parent directory.");
                }
            }
        }

        private string StartWebHostInBackgroundThread()
        {
            var serverStarted = new ManualResetEvent(false);

            new Thread(() =>
            {
                _host.Start();
                serverStarted.Set();
            }).Start();

            serverStarted.WaitOne();

            return _host.ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses.Single();
        }
    }
}