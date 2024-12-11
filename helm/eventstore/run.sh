#!/bin/bash

LOCAL_PORT=9211
TARGET_POD_PORT=2113
PIDS=()

PODS=$(kubectl get pods -o jsonpath="{.items[*].metadata.name}")

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

for POD in $PODS; do
    echo "Forwarding local port $LOCAL_PORT to pod $POD:$TARGET_POD_PORT"
    kubectl port-forward "pod/$POD" "$LOCAL_PORT:$TARGET_POD_PORT">/dev/null&
    
    PIDS+=($!)
    
    ((LOCAL_PORT++))
done

echo "All port forwards have been set up."

wait
