using System;
using System.Collections.Immutable;
using AustinHarris.JsonRpc;
using Newtonsoft.Json;

namespace stratum_server_sharp
{
    public static class SubscribeResponse
    {
        public static object Create(string notifyId, string extraNonce, int extraNonceSize) => new object[]
        {
            new[] {"mining.notify", notifyId},
            extraNonce,
            extraNonceSize,
        };
    }
    

    class StratumService : JsonRpcService
    {
        [JsonRpcMethod("mining.authorize")]
        private bool authorize(string username, string password) => true;

        [JsonRpcMethod]
        private int decr(int i)
        {
            return i - 1;
        }

        [JsonRpcMethod("mining.subscribe")]
        private object subscribe(string userAgent) => SubscribeResponse.Create("hoge", "010d3a11", 8);

        [JsonRpcMethod("mining.submit")]
        private bool submit(string username, string jobid, string extranonce2, string ntime) => true;

        [JsonRpcMethod("mining.multi_version")]
        private bool multiVersion(int param) => true;
    }
}