﻿using System;
using Vostok.Clusterclient.Context;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Tracing;
using Vostok.Hosting.Setup;

// ReSharper disable ParameterHidesMember

namespace Vostok.Hosting.Components.ClusterClient
{
    internal class ClusterClientSetupBuilder : IVostokClusterClientSetupBuilder, IBuilder<ClusterClientSetup>
    {
        private ClusterClientSetupTracingBuilder tracingBuilder;

        public ClusterClientSetupBuilder()
        {
            tracingBuilder = new ClusterClientSetupTracingBuilder();
        }

        public ClusterClientSetup Build(BuildContext context)
        {
            ClusterClientSetup setup = s =>
            {
                s.SetupDistributedContext();
                s.SetupDistributedTracing(tracingBuilder.Build(context));
            };

            return setup;
        }

        public IVostokClusterClientSetupBuilder SetupTracing(Action<IVostokClusterClientSetupTracingBuilder> tracingSetup)
        {
            tracingSetup(tracingBuilder);
            return this;
        }
    }
}