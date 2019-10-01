﻿using JetBrains.Annotations;
using Vostok.Metrics;

namespace Vostok.Hosting.Setup
{
    [PublicAPI]
    public interface IMetricsBuilder
    {
        IMetricsBuilder SetupHerculesMetricEventSender([NotNull] EnvironmentSetup<IHerculesMetricEventSenderBuilder> herculesMetricEventSenderSetup);

        IMetricsBuilder AddMetricEventSenderSender([NotNull] IMetricEventSender metricEventSender);
    }
}