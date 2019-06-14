#include "mhttp.ios.h"
#include "TwitterNetworkLayer/TwitterNetworkLayer.h"

@implementation MhttpAppDelegateHelper

#if TARGET_OS_IPHONE // == IOS + WATCH + TV
+ (void)handleBackgroundURLSessionEvents:(nullable NSString *)identifier
completionHandler:(dispatch_block_t)completionHandler
{
    if (![TNLRequestOperationQueue handleBackgroundURLSessionEvents:identifier completionHandler:completionHandler]) {
        completionHandler();
    }
}
#endif

@end

