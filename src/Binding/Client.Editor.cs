using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Mhttp {
#if UNITY_EDITOR
    // Mhttp for editor is just avoiding compile error. no http2
    public partial class Client {
        const int PROCESSING_PER_LOOP = 10;
        public class ResponseImpl : Response {
            Request request_;
            UnityWebRequest response_;

            internal ResponseImpl(UnityWebRequest webrequest, Request r) {
                request_ = r;
                response_ = webrequest;
            }

            // implements Response
            public Request request {
                get {
                    return request_;
                }
            }

            public int code {
                get {
                    return (int)response_.responseCode;
                }
            }
            public string error {
                get {
                    return response_.error;
                }
            }
            public byte[] data {
                get {
                    return response_.downloadHandler.data;
                }
            }
            public string header(string key) {
                return response_.GetResponseHeader(key);
            }
            public bool isDone {
                get {
                    return response_.downloadHandler.isDone;
                }
            }
        }

        static public Response Send(Request r) {
            var resp = new UnityWebRequest(r.url);
            foreach (var kv in r.headers) {
                resp.SetRequestHeader(kv.Key, kv.Value);
            }
            if (r.method == null) {
                if (r.body != null) {
                    resp.method = "GET";
                } else {
                    resp.method = "POST";
                }
            } else {
                resp.method = r.method;
            }
            if (r.body != null) {
                resp.uploadHandler = (UploadHandler)new UploadHandlerRaw(r.body);
            }
            resp.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            return new ResponseImpl(resp, r);
        }

        static public void Update() {
        }
    }
#endif
}
