#!/bin/bash

# Cozy Comfort - Stop All Services Script

echo "Stopping Cozy Comfort Services..."

if [ -d ".pids" ]; then
    if [ -f ".pids/manufacturer.pid" ]; then
        PID=$(cat .pids/manufacturer.pid)
        if ps -p $PID > /dev/null 2>&1; then
            kill $PID
            echo "Stopped ManufacturerService (PID: $PID)"
        fi
    fi
    
    if [ -f ".pids/distributor.pid" ]; then
        PID=$(cat .pids/distributor.pid)
        if ps -p $PID > /dev/null 2>&1; then
            kill $PID
            echo "Stopped DistributorService (PID: $PID)"
        fi
    fi
    
    if [ -f ".pids/seller.pid" ]; then
        PID=$(cat .pids/seller.pid)
        if ps -p $PID > /dev/null 2>&1; then
            kill $PID
            echo "Stopped SellerService (PID: $PID)"
        fi
    fi
    
    rm -rf .pids
fi

# Also kill any dotnet processes on our ports
echo "Killing processes on ports 5001, 5002, 5003..."
lsof -ti:5001 | xargs kill -9 2>/dev/null && echo "  Port 5001 cleared" || echo "  Port 5001 was free"
lsof -ti:5002 | xargs kill -9 2>/dev/null && echo "  Port 5002 cleared" || echo "  Port 5002 was free"
lsof -ti:5003 | xargs kill -9 2>/dev/null && echo "  Port 5003 cleared" || echo "  Port 5003 was free"

echo ""
echo "All services stopped."
