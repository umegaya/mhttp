#!/bin/bash

CWD=$(cd $(dirname $0) && pwd)
TARGET_ROOT=$CWD/../../../../package/iOS/

cp $CWD/mhttp/mhttp.ios.h $TARGET_ROOT
cp $CWD/libmhttp.a $TARGET_ROOT
cp $CWD/unity/*.mm $TARGET_ROOT
