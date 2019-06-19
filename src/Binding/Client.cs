using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Mhttp {
    public static partial class Client {
        public class Request {
            public string url = null;
            public byte[] body = null;
            public string method = null;
            public Dictionary<string, string> headers = null;
            public Action<Response> callback = null;
        }

        public interface Response {
            Request request { get; set; }
            int code { get; }
            string error { get; }
            byte[] data { get; }
            string header(string key);
            bool isDone { get; }
        }

        static public Response Send(Request r) {
            List<string> headers = new List<string>();
            if (r.body != null && r.body.Length > 0) {
                headers.Add("Content-Length");
                headers.Add(r.body.Length.ToString());
            }
            if (r.headers != null) {
                foreach (var kv in r.headers) {
                    headers.Add(kv.Key);
                    headers.Add(kv.Value);
                }
            }
            var resp = Send(r.url, r.method, headers.ToArray(), r.body);
            resp.request = r;
            return resp;
        }
    }
}
