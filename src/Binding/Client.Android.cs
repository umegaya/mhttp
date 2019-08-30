using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Mhttp {
#if UNITY_ANDROID && !UNITY_EDITOR
    public partial class Client {
        const int PROCESSING_PER_LOOP = 10;
        public class ResponseImpl : Response, IDisposable {
            string uuid_;

            internal ResponseImpl(string uuid) {
                uuid_ = uuid;
                isDone = false;
            }

            ~ResponseImpl() {
                Dispose();
            }

            public void Dispose() {
                if (uuid_ != null) {
                    // Debug.Log("dispose:" + uuid_);
                    Client.dispose_queue_.Enqueue(uuid_);
                    uuid_ = null;
                }
            }

            public void Done() {
                isDone = true;
            }

            // implements Response
            public Request request { get; set; }

            public int code {
                get {
                    return Client.client_.Call<int>("code", uuid_);
                }
            }
            public string error {
                get {
                    return Client.client_.Call<string>("error", uuid_);
                }
            }
            public byte[] data {
                get {
                    AndroidJavaObject obj = Client.client_.Call<AndroidJavaObject>("body", uuid_);
                    if (obj.GetRawObject() != System.IntPtr.Zero) {
                        return AndroidJNI.FromByteArray(obj.GetRawObject());
                    } else {
                        return null;
                    }
                }
            }
            public string header(string key) {
                return Client.client_.Call<string>("header", uuid_, key);
            }
            public bool isDone {
                get; set;
            }
        }
        static AndroidJavaObject client_;
        static Dictionary<string, ResponseImpl> respmap_ = new Dictionary<string, ResponseImpl>();
        static Queue<string> dispose_queue_ = new Queue<string>();

        static Client() {
            AndroidJavaClass cls = new AndroidJavaClass("mhttp.Client");
            client_ = cls.CallStatic<AndroidJavaObject>("instance");
        }

        static public Response Send(
            string url,
            string method,
            string[] headers,
            byte[] body,
            Options options
        ) {
            var uuid = System.Guid.NewGuid().ToString();
            var resp = new ResponseImpl(uuid);
            respmap_[uuid] = resp;
            sbyte[] sbody = null;
            if (body != null) {
                // this wasteful code to prevent warning: 
                // AndroidJNIHelper.GetSignature: using Byte parameters is obsolete, use SByte parameters instead
                sbody = new sbyte[body.Length];
                Buffer.BlockCopy(body, 0, sbody, 0, body.Length);
            }
            client_.Call<string>("execute", uuid, url, method, headers, sbody, 
                options != null ? options.filepath : null
            );
            return resp;
        }

        static public void Update() {
            int n_process = 0;
            while (n_process < PROCESSING_PER_LOOP) {
                var finished = client_.Call<string>("popFinished");
                if (finished == null) {
                    break;
                }
                ResponseImpl resp;
                if (respmap_.TryGetValue(finished, out resp)) { 
                    resp.Done();
                    if (resp.request.callback != null) {
                        resp.request.callback(resp);
                    }
                }
                respmap_.Remove(finished);
                n_process++;
            }
            for (int i = 0; i < dispose_queue_.Count && n_process < PROCESSING_PER_LOOP; i++) {
                string finished = dispose_queue_.Dequeue();
                client_.Call("endResponse", finished);
                n_process++;
            }
        }
    }
    #endif
}