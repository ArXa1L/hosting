﻿using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Hercules.Client;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hosting.Components.ClusterProvider;
using Vostok.Hosting.Setup;

// ReSharper disable ParameterHidesMember

namespace Vostok.Hosting.Components.Hercules
{
    internal class HerculesSinkBuilder : IHerculesSinkBuilder, IBuilder<IHerculesSink>
    {
        private Func<string> apiKeyProvider;
        private IBuilder<IClusterProvider> clusterProviderBuilder;

        [NotNull]
        public IHerculesSink Build(Context context)
        {
            if (apiKeyProvider == null)
                return new DevNullHerculesSink();

            var cluster = clusterProviderBuilder.Build(context);
            if (cluster == null)
                return new DevNullHerculesSink();

            return new HerculesSink(new HerculesSinkSettings(cluster, apiKeyProvider), context.Log);
        }
        
        public IHerculesSinkBuilder SetApiKeyProvider(Func<string> apiKeyProvider)
        {
            this.apiKeyProvider = apiKeyProvider;
            return this;
        }

        public IHerculesSinkBuilder SetClusterConfigClusterProvider(string path)
        {
            clusterProviderBuilder = new ClusterConfigClusterProviderBuilder(path);
            return this;
        }

        public IHerculesSinkBuilder SetServiceDiscoveryClusterProvider(string environment, string application)
        {
            clusterProviderBuilder = new ServiceDiscoveryClusterProviderBuilder(environment, application);
            return this;
        }

        public IHerculesSinkBuilder SetClusterProvider(IClusterProvider clusterProvider)
        {
            clusterProviderBuilder = new CustomClusterProviderBuilder(clusterProvider);
            return this;
        }
    }
}