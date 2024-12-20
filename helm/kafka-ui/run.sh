#!/bin/bash

LOCAL_PORT=9091
TARGET_POD_PORT=80
SVC="kafka-ui-dev"
PIDS=()

cleanup() {
    echo "Cleaning up..."
    for PID in "${PIDS[@]}"; do
        if [ -n "$PID" ] && kill -0 $PID 2>/dev/null; then
            echo "Killing process $PID"
            kill -15 $PID
        fi
    done
    wait
    echo "All cleanup tasks have been completed."
}

trap 'cleanup' EXIT

echo "Forwarding local port $LOCAL_PORT to svc $SVC:$TARGET_POD_PORT"
kubectl port-forward "svc/$SVC" "$LOCAL_PORT:$TARGET_POD_PORT">/dev/null
PIDS+=($!)

echo "All port forwards have been set up."

wait
