#import "UnityAppController.h"
#import "mhttp.ios.h"

@interface MhttpAppController : UnityAppController
@end

@implementation MhttpAppController
- (void)application:(nonnull UIApplication *)application handleEventsForBackgroundURLSession:(nonnull NSString *)identifier completionHandler:(nonnull void (^)(void))completionHandler
{
    NSLog(@"%@ %@", NSStringFromSelector(_cmd), identifier);
    [MhttpAppDelegateHelper handleBackgroundURLSessionEvents:identifier completionHandler:completionHandler];
}
@end

IMPL_APP_CONTROLLER_SUBCLASS(MhttpAppController)
