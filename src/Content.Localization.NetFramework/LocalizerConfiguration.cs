﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization
{

    /// <summary>
    /// Poor man's DI builder
    /// </summary>
    public class LocalizerConfiguration : IContentBuilder
    {
        internal List<IContentSource> Sources { get; } = new List<IContentSource>();

        internal Lazy<ContentUpdater> UpdaterFactory {get; set;}

        internal IContentClassGenerator ClassGenerator { get; set; }

        public IContentLogger ContentLogger { get; set; } = new NullContentLogger();

        

        public IContentBuilder AddContentSource<TContentSource>(Func<TContentSource> factory) where TContentSource : IContentSource
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Sources.Add( factory() );

            return this;
        }

        public IContentBuilder AddLogger<TLogger>(Func<TLogger> factory) where TLogger : IContentLogger
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            ContentLogger = factory();
            return this;
        }
    }



}
