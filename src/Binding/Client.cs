using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Mhttp {
    public static partial class Client {
        public class Options {
            public string filepath = null; // if non null value specified, write response to this path
        }
        public class Request {
            public string url = null;
            public byte[] body = null;
            public string method = null;
            public Dictionary<string, string> headers = null;
            public Action<Response> callback = null;
            public Options options = null;
        }

        public interface Response {
            Request request { get; set; }
            int code { get; }
            string error { get; }
            byte[] data { get; }
            string header(string key);
            bool isDone { get; }

            void Abort();
        }

        static public Response Send(Request r) {
            List<string> headers = new List<string>();
            if (r.headers != null) {
                foreach (var kv in r.headers) {
                    headers.Add(kv.Key);
                    headers.Add(kv.Value);
                }
            }
            var resp = Send(r.url, r.method, headers.ToArray(), r.body, r.options);
            resp.request = r;
            return resp;
        }
    }
}
