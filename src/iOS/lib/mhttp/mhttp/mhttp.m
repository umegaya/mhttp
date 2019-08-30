#include "mhttp.h"
#include "TwitterNetworkLayer/TwitterNetworkLayer.h"

NSString *MhttpCommunicationStatusUpdatedNotification = @"MhttpCommunicationStatusUpdatedNotification";

@interface MhttpDelegate : NSObject<TNLNetworkObserver, TNLLogger, TNLCommunicationAgentObserver>
@end

@implementation MhttpDelegate
{
    TNLCommunicationAgent *_commAgent;
    NSString *_communicationStatusDescription;
    NSString *_SCFlagsString;
    NSString *_statusString;
    NSString *_carrierName;
    NSString *_radioTech;
    TNLLogLevel _logLevel;
    bool _connected;
}

- (instancetype)initWithConnectivityHost:(NSString *)hostname
                                logLevel:(TNLLogLevel)logLevel
{
    // Set up logging
    [TNLGlobalConfiguration sharedInstance].logger = self;
    
    // Prepare network "business" observer
    [[NSNotificationCenter defaultCenter] addObserver:self
                                             selector:@selector(networkingDidChange:)
                                            name:TNLNetworkExecutingNetworkConnectionsDidUpdateNotification
                                               object:nil];
    
    // Prepare global settings
    _commAgent = [[TNLCommunicationAgent alloc] initWithInternetReachabilityHost:hostname];
    [[TNLGlobalConfiguration sharedInstance] addNetworkObserver:self];
    [[TNLGlobalConfiguration sharedInstance] setAssertsEnabled:YES];
    [[TNLGlobalConfiguration sharedInstance] setMetricProvidingCommunicationAgent:_commAgent];
    [_commAgent addObserver:self];
    
    _logLevel = logLevel;
    
    return self;
}

- (void)networkingDidChange:(NSNotification *)note
{
    assert([NSThread isMainThread]);
    _connected = [note.userInfo[TNLNetworkExecutingNetworkConnectionsExecutingKey] boolValue];
}

- (BOOL)tnl_canLogWithLevel:(TNLLogLevel)level context:(id)context
{
    return level <= TNLLogLevelDebug;
}

- (void)tnl_logWithLevel:(TNLLogLevel)level context:(id)context file:(NSString *)file function:(NSString *)function line:(int)line message:(NSString *)message
{
    if (level > _logLevel) {
        return;
    }
    NSString *levelString = nil;
    switch (level) {
        case TNLLogLevelEmergency:
        case TNLLogLevelAlert:
        case TNLLogLevelCritical:
        case TNLLogLevelError:
            levelString = @"ERR";
            break;
        case TNLLogLevelWarning:
            levelString = @"WRN";
            break;
        case TNLLogLevelNotice:
        case TNLLogLevelInformation:
            levelString = @"INF";
            break;
        case TNLLogLevelDebug:
            levelString = @"DBG";
            break;
    }
    
    NSLog(@"[%@]: %@", levelString, message);
}

- (BOOL)tnl_shouldRedactHTTPHeaderField:(nonnull NSString *)headerField
{
    if ([headerField isEqualToString:@"Authorization"]) {
        return YES;
    }
    return NO;
}

- (void)tnl_requestOperation:(TNLRequestOperation *)op
didCompleteWithResponse:(TNLResponse *)response
{
    TNLAttemptMetrics *lastAttemptMetrics = response.metrics.attemptMetrics.lastObject;
    TNLAttemptMetaData *metaData = lastAttemptMetrics.metaData;
    
    int64_t downloadByteCount = metaData.layer8BodyBytesReceived;
    NSTimeInterval duration = response.metrics.totalDuration;
    BOOL isCached = response.info.source == TNLResponseSourceLocalCache;
    BOOL errorWasEncountered = response.operationError != nil;
    if (downloadByteCount < 0 || duration <= 0 || isCached) {
        return;
    }
    
    double bytes = downloadByteCount;
    double bps = bytes/duration;
    
    static NSByteCountFormatter *bpsFormatter;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        bpsFormatter = [[NSByteCountFormatter alloc] init];
        bpsFormatter.countStyle = NSByteCountFormatterCountStyleBinary;
        bpsFormatter.allowedUnits = NSByteCountFormatterUseKB;
        bpsFormatter.zeroPadsFractionDigits = YES;
        bpsFormatter.adaptive = YES;
    });
    NSLog(@"Bandwidth - %@ / %.2fs = %@ps%@", [bpsFormatter stringFromByteCount:downloadByteCount], duration, isnan(bps) ?@"NaN B" : [bpsFormatter stringFromByteCount:(long long)bps], errorWasEncountered ? @" DNF!" : @"");
}

#pragma mark TNLCommunicationAgentObserver

- (void)tnl_communicationAgent:(TNLCommunicationAgent *)agent
didRegisterObserverWithInitialReachabilityFlags:(SCNetworkReachabilityFlags)flags
status:(TNLNetworkReachabilityStatus)status
carrierInfo:(nullable id<TNLCarrierInfo>)info
WWANRadioAccessTechnology:(nullable NSString *)radioTech
captivePortalStatus:(TNLCaptivePortalStatus)captivePortalStatus
{
    _SCFlagsString = TNLDebugStringFromNetworkReachabilityFlags(flags);
    _statusString = TNLNetworkReachabilityStatusToString(status) ?: @"<null>";
    _carrierName = info.carrierName ?: @"<null>";
    _radioTech = [radioTech stringByReplacingOccurrencesOfString:@"CTRadioAccessTechnology" withString:@""] ?: @"<null>";
    
    NSDictionary *logInfo = @{
        @"SC_flags" : _SCFlagsString,
        @"status" : _statusString,
        @"carrier" : info ?: (id)@"<null>",
        @"radioTech" : _radioTech,
    };
    NSLog(@"did register: %@", logInfo);
}

- (void)tnl_communicationAgent:(TNLCommunicationAgent *)agent
didUpdateReachabilityFromPreviousFlags:(SCNetworkReachabilityFlags)oldFlags
previousStatus:(TNLNetworkReachabilityStatus)oldStatus
toCurrentFlags:(SCNetworkReachabilityFlags)newFlags
currentStatus:(TNLNetworkReachabilityStatus)newStatus
{
    _SCFlagsString = TNLDebugStringFromNetworkReachabilityFlags(newFlags);
    _statusString = TNLNetworkReachabilityStatusToString(newStatus) ?: @"<null>";
    
    NSDictionary *logInfo = @{
        @"SC_flags_old" : TNLDebugStringFromNetworkReachabilityFlags(oldFlags),
        @"SC_flags_new" : _SCFlagsString,
        @"status_old" : TNLNetworkReachabilityStatusToString(oldStatus) ?: @"<null>",
        @"status_new" : _statusString,
    };
    NSLog(@"did update reachability: %@", logInfo);
}

- (void)tnl_communicationAgent:(TNLCommunicationAgent *)agent
didUpdateCarrierFromPreviousInfo:(nullable id<TNLCarrierInfo>)oldInfo
toCurrentInfo:(nullable id<TNLCarrierInfo>)newInfo
{
    _carrierName = newInfo.carrierName ?: @"<null>";
    
    NSDictionary *logInfo = @{
        @"carrier_old" : oldInfo ?: (id)@"<null>",
        @"carrier_new" : newInfo ?: (id)@"<null>",
    };
    NSLog(@"did update carrier: %@", logInfo);
}

- (void)tnl_communicationAgent:(TNLCommunicationAgent *)agent
didUpdateWWANRadioAccessTechnologyFromPreviousTech:(nullable NSString *)oldTech
toCurrentTech:(nullable NSString *)newTech
{
    _radioTech = [newTech stringByReplacingOccurrencesOfString:@"CTRadioAccessTechnology" withString:@""] ?: @"<null>";
    
    NSDictionary *logInfo = @{
        @"radioTech_old" : oldTech ?: @"null",
        @"radioTech_new" : newTech ?: @"null",
    };
    NSLog(@"did update radio tech: %@", logInfo);
}

- (NSString *)communicationStatusDescription
{
    return _communicationStatusDescription;
}

@end

static NSInteger get_verb(const char *method, const char *body) {
    const char *verb = method;
    if (verb == NULL) {
        if (body != NULL) {
            return TNLHTTPMethodPOST;
        } else {
            return TNLHTTPMethodGET;
        }
    }
    if (strcmp(verb, "GET") == 0) {
        return TNLHTTPMethodGET;
    } else if (strcmp(verb, "POST") == 0) {
        return TNLHTTPMethodPOST;
    } else if (strcmp(verb, "PUT") == 0) {
        return TNLHTTPMethodPUT;
    } else if (strcmp(verb, "DELETE") == 0) {
        return TNLHTTPMethodDELETE;
    } else if (strcmp(verb, "OPTIONS") == 0) {
        return TNLHTTPMethodOPTIONS;
    } else if (strcmp(verb, "HEAD") == 0) {
        return TNLHTTPMethodHEAD;
    } else if (strcmp(verb, "TRACE") == 0) {
        return TNLHTTPMethodTRACE;
    } else if (strcmp(verb, "CONNECT") == 0) {
        return TNLHTTPMethodCONNECT;
    } else {
        return TNLHTTPMethodUnknown;
    }
}


struct _mhttp_conn {
    MhttpDelegate *delegate;
    NSMutableDictionary *valid_responses;
};

mhttp_conn_t mhttp_connect(const char *connectivity_host) {
    struct _mhttp_conn *c = malloc(sizeof(struct _mhttp_conn));
    c->delegate = [[MhttpDelegate alloc] initWithConnectivityHost:[NSString stringWithUTF8String:connectivity_host] logLevel:TNLLogLevelWarning];
    c->valid_responses = [NSMutableDictionary dictionary];
    return (mhttp_conn_t)c;
}

mhttp_response_t *mhttp_request(mhttp_conn_t c,
                                const char *url,
                                const char *method,
                                const char **headers,
                                uint64_t headers_len,
                                const char *body,
                                uint64_t body_len,
                                mhttp_options_t *options,
                                mhttp_closure_t *cb) {
    NSURL *URL = [NSURL URLWithString:[NSString stringWithUTF8String:url]];
    TNLMutableHTTPRequest *request = [[TNLMutableHTTPRequest alloc] init];
    request.HTTPMethodValue = get_verb(method, body);
    request.URL = URL;
    [request setValue:[NSString stringWithFormat:@"mhttp/tnl-%f", TNL_PROJECT_VERSION] forHTTPHeaderField:@"User-Agent"];
    if (headers != NULL) {
        for (int i = 0; i < headers_len; i+=2) {
            [request setValue:[NSString stringWithUTF8String:headers[i+1]] forHTTPHeaderField:[NSString stringWithUTF8String:headers[i]]];
        }
    }
    if (body != NULL) {
        [request setHTTPBody:[NSData dataWithBytes:(const void *)body length:body_len]];
    }
    mhttp_response_t *resp = calloc(1, sizeof(mhttp_response_t));
    if (cb != NULL) {
        resp->cb = *cb;
    }
    if (options != NULL) {
        if (options->filepath != NULL) {
            resp->options.filepath = strdup(options->filepath);
        } else {
            // resp is calloced, so no need to do this
            // resp->options.filepath = NULL;
        }
    }
    NSNumber *addr = [NSNumber numberWithInteger:(NSInteger)resp];
    @synchronized (c->valid_responses) {
        [c->valid_responses setObject:addr forKey:addr];
    }
    [[TNLRequestOperationQueue defaultOperationQueue] enqueueRequest:request
                                                          completion:^(TNLRequestOperation *op, TNLResponse *response) {
        @synchronized (c->valid_responses) {
            if ([c->valid_responses objectForKey:addr] == nil) {
                return;
            }
            // this part does not assure that same thread as Unity's main thread.
            // so copy over every memory from TNLResponse. it may huge overhead but no choice.
            if (response.operationError != NULL) {
                resp->error = strdup([response.operationError.debugDescription UTF8String]);
                resp->error_len = [response.operationError.debugDescription length];
                resp->status = -1;
            } else {
                resp->status = response.info.statusCode;
                NSDictionary *headers = response.info.allHTTPHeaderFields;
                if (headers != NULL) {
                    size_t hdlen = [headers count] * 2;
                    resp->headers = malloc(sizeof(char *) * hdlen);
                    int count = 0;
                    for(id key in headers) {
                        // NSLog(@"key=%@ value=%@", key, [response.info.allHTTPHeaderFields objectForKey:key]);
                        NSString *hd_name = (NSString *)key;
                        NSString *hd_value = (NSString *)[response.info.allHTTPHeaderFields objectForKey:key];
                        resp->headers[count] = strdup([hd_name UTF8String]);
                        resp->headers[count + 1] = strdup([hd_value UTF8String]);
                        count += 2;
                    }
                    resp->headers_len = hdlen;
                }
                if (response.info.data != NULL) {
                    if (resp->options.filepath != NULL) {
                        [response.info.data writeToFile:[NSString stringWithUTF8String:resp->options.filepath]
                                             atomically:TRUE];
                        // resp->body and body_len is already zero filled
                    } else {
                        NSInteger length = [response.info.data length];
                        void *body = malloc(length);
                        memcpy(body, [response.info.data bytes], length);
                        resp->body = body;
                        resp->body_len = length;
                    }
                }
                if (resp->cb.cb != NULL) {
                    resp->cb.cb(resp->cb.arg, c, resp);
                }
            }
            // eventually Unity main thread knows finished flag is on
            resp->finished = 1;
        }
    }];
    return resp;
}

const char *mhttp_response_header(mhttp_response_t *resp, const char *key) {
    if (resp->headers == NULL) {
        return NULL;
    }
    for (int i = 0; i < resp->headers_len; i += 2) {
        if (strcmp(key, resp->headers[i]) == 0) {
            return resp->headers[i + 1];
        }
    }
    return NULL;
}

void mhttp_response_end(mhttp_conn_t c, mhttp_response_t *resp) {
    @synchronized (c->valid_responses) {
        NSNumber *addr = [NSNumber numberWithInteger:(NSInteger)resp];
        [c->valid_responses removeObjectForKey:addr];
    }
    if (resp->body != NULL) {
        free((void *)resp->body);
    }
    if (resp->error != NULL) {
        free((void *)resp->error);
    }
    if (resp->headers != NULL) {
        for (int i = 0; i < resp->headers_len; i++) {
            free((void *)resp->headers[i]);
        }
        free(resp->headers);
    }
    if (resp->options.filepath != NULL) {
        free((void *)resp->options.filepath);
    }
    free(resp);
}

