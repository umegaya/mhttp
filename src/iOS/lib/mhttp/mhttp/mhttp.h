#pragma once

#include <stdint.h>

typedef struct _mhttp_conn *mhttp_conn_t;
extern mhttp_conn_t mhttp_connect(const char *hostname);

struct _mhttp_response;
typedef struct _mhttp_closure {
    void *arg;
    void (*cb)(void *arg, mhttp_conn_t c, const struct _mhttp_response *body);
} mhttp_closure_t;
struct _mhttp_response {
    int32_t status;
    volatile int32_t finished;
    uint64_t headers_len;
    const char **headers;
	const char *body;
    uint64_t body_len;
	const char *error;
    mhttp_closure_t cb;
};
typedef struct _mhttp_response mhttp_response_t;

typedef struct _mhttp_request {
	const char *url;
	const char *method;
	const char **headers;
    uint64_t headers_len;
	const char *body;
    uint64_t body_len;
	mhttp_closure_t cb;
} mhttp_request_t;
extern mhttp_response_t *mhttp_request(mhttp_conn_t c, const mhttp_request_t *req);

extern const char *mhttp_response_header(mhttp_response_t *resp, const char *key);
extern void mhttp_response_end(mhttp_conn_t c, mhttp_response_t *resp);
