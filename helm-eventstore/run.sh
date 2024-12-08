#!/bin/bash

# 定义起始的本地端口号
LOCAL_PORT=9211

# 定义要转发的目标 Pod 端口
TARGET_POD_PORT=2113

# 用于存储 kubectl port-forward 进程 PID 的数组
PIDS=()

# 获取所有匹配选择器的 Pod 名称
PODS=$(kubectl get pods -l app=esdb -o jsonpath="{.items[*].metadata.name}")

# 清理函数：终止所有转发进程
cleanup() {
    # echo "Cleaning up..."
    for PID in "${PIDS[@]}"; do
        if [ -n "$PID" ] && kill -0 $PID 2>/dev/null; then
            echo "Killing process $PID"
            kill -15 $PID
        fi
    done
    wait
}

# 捕获中断信号 (Ctrl+C) 和脚本退出事件
trap 'cleanup' INT EXIT

# 循环遍历所有 Pod 并设置端口转发
for POD in $PODS; do
    echo "Forwarding local port $LOCAL_PORT to pod $POD:$TARGET_POD_PORT"
    kubectl port-forward "pod/$POD" "$LOCAL_PORT:$TARGET_POD_PORT">/dev/null&
    
    # 将新启动的进程 ID 添加到 PIDS 数组中
    PIDS+=($!)
    
    # 每次循环后增加本地端口号
    ((LOCAL_PORT++))
done

echo "All port forwards have been set up."

# 让主脚本等待后台进程完成（这里可以替换为你想执行的任务）
wait
echo "All cleanup tasks have been completed."
