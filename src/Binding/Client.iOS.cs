using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using Marshal = System.Runtime.InteropServices.Marshal;

using UnityEngine;

namespace Mhttp {
#if UNITY_IOS
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
                    // Debug.Log("dispose:" + uuid_);
                    mhttp_response_end(handle_);
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
                            byte[] err = new byte[pnr->err_len];
                            Marshal.Copy(pnr->error, err, 0, (int)pnr->err_len);
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
                            body_ = new byte[pnr->err_len];
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
            System.IntPtr resp_handle = System.IntPtr.Zero;
            unsafe {
                fixed (byte *body = r.body) {
                    var nr = new NativeRequest {
                        url = r.url,
                        method = r.method,
                        body = body,
                        body_len = (ulong)r.body.Length,
                    };
                    if (r.headers != null) {
                        System.IntPtr[] headers = new System.IntPtr[r.headers.Count * 2];
                        int hd_len = 0;
                        foreach (var kv in r.headers) {
                            headers[hd_len] = Marshal.StringToCoTaskMemAnsi(kv.Key);
                            headers[hd_len + 1] = Marshal.StringToCoTaskMemAnsi(kv.Value);
                            hd_len += 2;
                        }
                        nr.headers = headers;
                        nr.headers_len = (ulong)hd_len;
                    }
                    resp_handle = mhttp_request(client_, ref nr);
                    nr.Free();
                }
            }
            return new ResponseImpl(resp_handle, r);
        }

        static public void Update() {
        }
    }
#endif
}
