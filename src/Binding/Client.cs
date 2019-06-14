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
            Request request { get; }
            int code { get; }
            string error { get; }
            byte[] data { get; }
            string header(string key);
            bool isDone { get; }
        }
    }
}
