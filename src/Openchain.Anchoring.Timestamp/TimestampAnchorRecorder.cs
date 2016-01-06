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

namespace Openchain.Anchoring.Timestamp
{
    /// <summary>
    /// Records database anchors in a Blockchain.
    /// </summary>
    public class TimestampAnchorRecorder : IAnchorRecorder
    {
        private static readonly byte[] anchorMarker = new byte[] { 0x4f, 0x43 };
        private readonly Uri url;
        private readonly string provider;
        private readonly string partyId;

        private ILogger logger;

        public TimestampAnchorRecorder(Uri url, string provider, string partyId, ILogger logger)
        {
            this.url = url;
            this.provider = provider;
            this.partyId = partyId;
            this.logger = logger;
        }

        /// <summary>
        /// Indicates whether this instance is ready to record a new database anchor.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<bool> CanRecordAnchor()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Records a database anchor.
        /// </summary>
        /// <param name="anchor">The anchor to be recorded.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<LedgerAnchorProof> RecordAnchor(LedgerAnchor anchor)
        {
            byte[] anchorPayload =
                anchorMarker
                .Concat(BitConverter.GetBytes((ulong)anchor.TransactionCount).Reverse())
                .Concat(anchor.FullStoreHash.ToByteArray())
                .ToArray();

            byte[] hash;
            using (var s = SHA256.Create()) hash=s.ComputeHash(anchorPayload);

            var tsrg = new TimeStampRequestGenerator();
            tsrg.SetCertReq(true);
            var tsr=tsrg.Generate(TspAlgorithms.Sha256, hash);

            using (HttpClient client = new HttpClient())
            {
                var content = new ByteArrayContent(tsr.GetEncoded());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");

                var resp=await client.PostAsync(url,content);

                resp.EnsureSuccessStatusCode();

                var e = await resp.Content.ReadAsByteArrayAsync();
                var tsresp = new TimeStampResponse(e);
                tsresp.Validate(tsr);
                return new LedgerAnchorProof(anchor.Position, provider, partyId, new ByteString(tsresp.TimeStampToken.GetEncoded()));                    
            }
        }
    }
}
