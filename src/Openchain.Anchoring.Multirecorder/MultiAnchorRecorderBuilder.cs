// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Openchain.Infrastructure;
using System.Collections.Generic;

namespace Openchain.Anchoring.Multi
{
    public class MultiAnchorRecorderBuilder : IComponentBuilder<MultiAnchorRecorder>
    {

        List<Func<IServiceProvider,IAnchorRecorder>> builders;

        public string Name { get; } = "Multi";

        public MultiAnchorRecorder Build(IServiceProvider serviceProvider)
        {
            return new MultiAnchorRecorder(builders.Select(x=>x(serviceProvider)).Where(x=>x!=null).ToArray(), serviceProvider.GetRequiredService<ILogger>());
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            var resolver = serviceProvider.GetService<IResolver>();
            var l = new List<Func<IServiceProvider, IAnchorRecorder>>();
            foreach (var section in configuration.GetSection("recorders").GetChildren())
            {
                var factory=await resolver.Create<IAnchorRecorder>(serviceProvider, section.Path);
                l.Add(factory);
            }
            builders = l;
        }
    }
}
