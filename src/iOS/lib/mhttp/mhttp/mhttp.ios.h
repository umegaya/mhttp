#import <dispatch/object.h>

@interface MhttpAppDelegateHelper : NSObject

+ (void)handleBackgroundURLSessionEvents:(nullable NSString *)identifier
                       completionHandler:(dispatch_block_t _Nonnull )completionHandler;

@end
