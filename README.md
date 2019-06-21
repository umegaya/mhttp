### mhttp
- small Unity Plugin to provide better latency and connectivity for http request

### under the food
- its thin wrapper of following matured http2 library of platform
  - iOS: [TNL](https://github.com/twitter/ios-twitter-network-layer)
  - Android: [okhttp](https://github.com/square/okhttp)
- for editor, its falled back to UnityWebRequest

### how to use it
- clone this repository
- open src/iOS/lib/mhttp/mhttp.xcodeproj
- build with release
- `cp -r` package directory to your Plugins folder
  - if Unity does not set correct `platform for plugin` for files under package directory, please set by yourself for now. 
  - PR to improve always welcome :D
