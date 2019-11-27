﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Helpers;
using Vostok.Configuration;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.Merging;
using Vostok.Configuration.Logging;
using Vostok.Configuration.Printing;
using Vostok.Configuration.Sources.Combined;
using Vostok.Configuration.Sources.Constant;
using Vostok.Hosting.Setup;

// ReSharper disable ParameterHidesMember

namespace Vostok.Hosting.Components.Configuration
{
    internal class ConfigurationBuilder : IVostokConfigurationBuilder, IBuilder<(IConfigurationSource, IConfigurationSource, IConfigurationProvider)>
    {
        private readonly List<IConfigurationSource> sources;
        private readonly List<IConfigurationSource> secretSources;

        private readonly Customization<SettingsMergeOptions> mergeSettingsCustomization;
        private readonly Customization<SettingsMergeOptions> mergeSecretSettingsCustomization;
        private readonly Customization<ConfigurationProviderSettings> configurationSettingsCustomization;
        private readonly Customization<PrintSettings> printSettingsCustomization;
        private readonly Customization<IVostokConfigurationContext> configurationContextCustomization;

        public ConfigurationBuilder()
        {
            sources = new List<IConfigurationSource>();
            secretSources = new List<IConfigurationSource>();
            mergeSettingsCustomization = new Customization<SettingsMergeOptions>();
            mergeSecretSettingsCustomization = new Customization<SettingsMergeOptions>();
            configurationSettingsCustomization = new Customization<ConfigurationProviderSettings>();
            printSettingsCustomization = new Customization<PrintSettings>();
            configurationContextCustomization = new Customization<IVostokConfigurationContext>();
        }

        public IVostokConfigurationBuilder AddSource(IConfigurationSource source)
        {
            sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        public IVostokConfigurationBuilder AddSecretSource(IConfigurationSource source)
        {
            secretSources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        public IVostokConfigurationBuilder CustomizeSettingsMerging(Action<SettingsMergeOptions> settingsCustomization)
        {
            mergeSettingsCustomization.AddCustomization(settingsCustomization ?? throw new ArgumentNullException(nameof(settingsCustomization)));
            return this;
        }

        public IVostokConfigurationBuilder CustomizeSecretSettingsMerging(Action<SettingsMergeOptions> settingsCustomization)
        {
            mergeSecretSettingsCustomization.AddCustomization(settingsCustomization ?? throw new ArgumentNullException(nameof(settingsCustomization)));
            return this;
        }

        public IVostokConfigurationBuilder CustomizeConfigurationProvider(Action<ConfigurationProviderSettings> settingsCustomization)
        {
            configurationSettingsCustomization.AddCustomization(settingsCustomization ?? throw new ArgumentNullException(nameof(settingsCustomization)));
            return this;
        }

        public IVostokConfigurationBuilder CustomizePrintSettings(Action<PrintSettings> settingsCustomization)
        {
            printSettingsCustomization.AddCustomization(settingsCustomization ?? throw new ArgumentNullException(nameof(settingsCustomization)));
            return this;
        }

        public IVostokConfigurationBuilder CustomizeConfigurationContext(Action<IVostokConfigurationContext> configurationContextCustomization)
        {
            this.configurationContextCustomization.AddCustomization(configurationContextCustomization ?? throw new ArgumentNullException(nameof(configurationContextCustomization)));
            return this;
        }

        public (IConfigurationSource, IConfigurationSource, IConfigurationProvider) Build(BuildContext context)
        {
            var mergeOptions = new SettingsMergeOptions();
            mergeSettingsCustomization.Customize(mergeOptions);
            var source = sources.Any()
                ? (IConfigurationSource)new CombinedSource(sources.ToArray(), mergeOptions)
                : new ConstantSource(null);

            var secretMergeOptions = new SettingsMergeOptions();
            mergeSettingsCustomization.Customize(secretMergeOptions);
            var secretSource = secretSources.Any()
                ? (IConfigurationSource)new CombinedSource(secretSources.ToArray(), secretMergeOptions)
                : new ConstantSource(null);

            var printSettings = new PrintSettings();
            printSettingsCustomization.Customize(printSettings);

            var providerSettings =
                new ConfigurationProviderSettings()
                    .WithErrorLogging(context.Log)
                    .WithSettingsLogging(context.Log, printSettings);
            configurationSettingsCustomization.Customize(providerSettings);

            var provider = new ConfigurationProvider(providerSettings);

            configurationContextCustomization.Customize(new ConfigurationContext(source, secretSource, provider, context.ClusterConfigClient));

            return (source, secretSource, provider);
        }
    }
}