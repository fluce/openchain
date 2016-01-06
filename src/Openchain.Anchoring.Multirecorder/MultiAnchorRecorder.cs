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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Openchain.Infrastructure;
using Org.BouncyCastle.Tsp;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Openchain.Anchoring.Multi
{
    /// <summary>
    /// Records database anchors in a Blockchain.
    /// </summary>
    public class MultiAnchorRecorder : IAnchorRecorder
    {
        private ILogger logger;
        private IAnchorRecorder[] recorders;

        public MultiAnchorRecorder(IAnchorRecorder[] recorders, ILogger logger)
        {
            this.logger = logger;
            this.recorders = recorders;
        }

        /// <summary>
        /// Indicates whether this instance is ready to record a new database anchor.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<bool> CanRecordAnchor()
        {
            return Task.FromResult(recorders.All(x=> { var t = x.CanRecordAnchor(); t.Wait(); return t.Result; }));
        }

        /// <summary>
        /// Records a database anchor.
        /// </summary>
        /// <param name="anchor">The anchor to be recorded.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<List<LedgerAnchorProof>> RecordAnchor(LedgerAnchor anchor)
        {
            return Task.FromResult(recorders.SelectMany(x => { var t = x.RecordAnchor(anchor); t.Wait(); return t.Result; }).ToList());
        }
    }
}
