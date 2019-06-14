using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using Marshal = System.Runtime.InteropServices.Marshal;

using UnityEngine;

namespace Mhttp {
#if UNITY_IOS
    public partial class Client {
        const string DllName = "__Internal";
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool ResponseCB(System.IntPtr arg, System.IntPtr conn, System.IntPtr resp);
        internal struct Closure {
            public System.IntPtr arg;
            public System.IntPtr cb;
        };
        internal unsafe struct NativeRequest {
            [MarshalAs(UnmanagedType.LPStr)]
            public string url;

            [MarshalAs(UnmanagedType.LPStr)]
            public string method;
            
            public System.IntPtr[] headers;
            
            public ulong headers_len;
            
            public byte *body;
            
            public ulong body_len;
            
            public Closure cb;

            public void Free() {
                if (headers != null) {
                    for (int i = 0; i < headers.Length; i++) {
                        if (headers[i] != System.IntPtr.Zero) {
                            Marshal.FreeCoTaskMem(headers[i]);
                        }
                    }
                }
            }
        }
        internal unsafe struct NativeResponse {
            public int status;
            public int finished;
            public ulong headers_len;
            public System.IntPtr p_headers;
            public System.IntPtr body;
            public ulong body_len;
            public System.IntPtr error;
            public ulong err_len;
            public Closure cb;
        }

        [DllImport (DllName)]
        private static extern System.IntPtr mhttp_connect([MarshalAs(UnmanagedType.LPStr)]string hostname);
        [DllImport (DllName)]
        private static extern unsafe System.IntPtr mhttp_request(System.IntPtr c, ref NativeRequest req);
        [DllImport (DllName)]
        private static extern unsafe void mhttp_response_end(System.IntPtr resp);

        [DllImport (DllName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string mhttp_response_header(System.IntPtr resp, [MarshalAs(UnmanagedType.LPStr)]string hkey);
    }
#endif
}
