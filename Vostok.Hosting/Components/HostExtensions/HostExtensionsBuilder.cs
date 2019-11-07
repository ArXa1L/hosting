﻿using System;
using Vostok.Commons.Helpers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Setup;

// ReSharper disable ParameterHidesMember

namespace Vostok.Hosting.Components.HostExtensions
{
    internal class HostExtensionsBuilder : IVostokHostExtensionsBuilder
    {
        public readonly HostExtensions HostExtensions;

        private readonly Customization<HostExtensionsBuilder> builderCustomization;
        private volatile IVostokHostingEnvironment environment;

        public HostExtensionsBuilder()
        {
            HostExtensions = new HostExtensions();
            builderCustomization = new Customization<HostExtensionsBuilder>();
        }

        public void AddCustomization(Action<IVostokHostExtensionsBuilder> setup)
            => builderCustomization.AddCustomization(setup);

        public void AddCustomization(Action<IVostokHostExtensionsBuilder, IVostokHostingEnvironment> setup)
            => builderCustomization.AddCustomization(b => setup(b, environment));

        public void Build(BuildContext context, IVostokHostingEnvironment environment)
        {
            this.environment = environment;

            if (context.ZooKeeperClient != null)
                HostExtensions.Add(context.ZooKeeperClient);

            builderCustomization.Customize(this);
        }

        public IVostokHostExtensionsBuilder Add<TExtension>(TExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));
            HostExtensions.Add(extension);
            return this;
        }

        public IVostokHostExtensionsBuilder Add<TExtension>(TExtension extension, string key)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));
            HostExtensions.Add(extension, key ?? throw new ArgumentNullException(nameof(key)));
            return this;
        }
    }
}