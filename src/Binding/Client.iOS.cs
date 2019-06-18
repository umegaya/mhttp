using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using Marshal = System.Runtime.InteropServices.Marshal;

using UnityEngine;

namespace Mhttp {
#if UNITY_IOS && !UNITY_EDITOR
    public partial class Client {
        const int PROCESSING_PER_LOOP = 10;
        public class ResponseImpl : Response, IDisposable {
            System.IntPtr handle_;
            Request request_;
            string error_ = null;
            byte[] body_ = null;

            internal ResponseImpl(System.IntPtr handle, Request r) {
                handle_ = handle;
                request_ = r;
            }

            ~ResponseImpl() {
                Dispose();
            }

            public void Dispose() {
                if (handle_ != System.IntPtr.Zero) {
                    // Debug.Log("dispose:" + ((ulong)handle_).ToString("X"));
                    mhttp_response_end(client_, handle_);
                    handle_ = System.IntPtr.Zero;
                }
            }

            // implements Response
            public Request request {
                get {
                    return request_;
                }
            }

            public int code {
                get {
                    unsafe {
                        NativeResponse *pnr = (NativeResponse *)handle_.ToPointer();
                        return pnr->status;
                    }
                }
            }
            public string error {
                get {
                    if (error_ == null) {
                        unsafe {
                            NativeResponse *pnr = (NativeResponse *)handle_.ToPointer();
                            if (pnr->error_len <= 0) {
                                return null;
                            }
                            byte[] err = new byte[pnr->error_len];
                            Marshal.Copy(pnr->error, err, 0, (int)pnr->error_len);
                            error_ = System.Text.Encoding.UTF8.GetString(err);
                        }
                    }
                    return error_;
                }
            }
            public byte[] data {
                get {
                    if (body_ == null) {
                        unsafe {
                            NativeResponse *pnr = (NativeResponse *)handle_.ToPointer();
                            if (pnr->body_len <= 0) {
                                return null;
                            }
                            body_ = new byte[pnr->body_len];
                            Marshal.Copy(pnr->body, body_, 0, (int)pnr->body_len);
                        }
                    }
                    return body_;
                }
            }
            public string header(string key) {
                return mhttp_response_header(handle_, key);
            }
            public bool isDone {
                get {
                    unsafe {
                        NativeResponse *pnr = (NativeResponse *)handle_.ToPointer();
                        return pnr->finished != 0;
                    }
                }
            }
        }
        static System.IntPtr client_;

        static Client() {
            client_ = mhttp_connect("www.google.com");
        }

        static public Response Send(Request r) {
            string[] headers = new string[(1 + (r.headers != null ? r.headers.Count : 0)) * 2];
            int hd_len = 0;
            if (r.body.Length > 0) {
                headers[0] = "Content-Length";
                headers[1] = r.body.Length.ToString();
                hd_len = 2; 
            }
            if (r.headers != null) {
                foreach (var kv in r.headers) {
                    headers[hd_len] = kv.Key;
                    headers[hd_len + 1] = kv.Value;
                    hd_len += 2;
                }
            }

            var resp_handle = mhttp_request(client_, 
                r.url,
                r.method,
                headers,
                hd_len,
                r.body,
                r.body.Length,
                null
            );
            return new ResponseImpl(resp_handle, r);
        }

        static public void Update() {
        }
    }
#endif
}
