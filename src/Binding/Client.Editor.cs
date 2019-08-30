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
            UnityWebRequest response_;

            internal ResponseImpl(UnityWebRequest webrequest) {
                response_ = webrequest;
                response_.SendWebRequest();
            }

            // implements Response
            public Request request { get; set; }

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
                    return response_.isDone;
                }
            }
        }

        static public Response Send(
            string url,
            string method,
            string[] headers,
            byte[] body,
            Options options
        ) {
            var resp = new UnityWebRequest(url);
            if (headers != null) {
                for (int i = 0; i < headers.Length; i+=2) {
                    resp.SetRequestHeader(headers[i], headers[i + 1]);
                }
            }
            if (method == null) {
                if (body != null) {
                    resp.method = "POST";
                } else {
                    resp.method = "GET";
                }
            } else {
                resp.method = method;
            }
            if (body != null) {
                resp.uploadHandler = (UploadHandler)new UploadHandlerRaw(body);
            }
            if (options != null && options.filepath != null) {
                resp.downloadHandler = (DownloadHandler)new DownloadHandlerFile(options.filepath);
            } else {
                resp.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            }
            return new ResponseImpl(resp);
        }

        static public void Update() {
        }
    }
#endif
}
