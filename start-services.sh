#!/bin/bash

# Cozy Comfort - Start All Services Script
# This script starts all three services in the background

echo "=========================================="
echo "  Starting Cozy Comfort Services"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if port is available
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
        return 1
    fi
    return 0
}

# Function to kill process on port
kill_port() {
    local port=$1
    local pids=$(lsof -ti:$port 2>/dev/null)
    if [ ! -z "$pids" ]; then
        echo -e "${YELLOW}Killing process(es) on port $port (PIDs: $pids)${NC}"
        kill -9 $pids 2>/dev/null
        sleep 1
    fi
}

# Check ports
echo "Checking ports..."
PORTS_IN_USE=false

if ! check_port 5001; then
    PIDS=$(lsof -ti:5001 2>/dev/null)
    echo -e "${YELLOW}Warning: Port 5001 is already in use (PIDs: $PIDS)${NC}"
    PORTS_IN_USE=true
fi

if ! check_port 5002; then
    PIDS=$(lsof -ti:5002 2>/dev/null)
    echo -e "${YELLOW}Warning: Port 5002 is already in use (PIDs: $PIDS)${NC}"
    PORTS_IN_USE=true
fi

if ! check_port 5003; then
    PIDS=$(lsof -ti:5003 2>/dev/null)
    echo -e "${YELLOW}Warning: Port 5003 is already in use (PIDs: $PIDS)${NC}"
    PORTS_IN_USE=true
fi

if [ "$PORTS_IN_USE" = true ]; then
    echo ""
    echo -e "${YELLOW}Some ports are already in use.${NC}"
    echo "Options:"
    echo "  1) Kill existing processes and continue"
    echo "  2) Exit and stop existing services manually"
    echo ""
    read -p "Enter choice (1 or 2): " choice
    
    if [ "$choice" = "1" ]; then
        echo ""
        echo "Killing processes on ports 5001, 5002, 5003..."
        kill_port 5001
        kill_port 5002
        kill_port 5003
        echo -e "${GREEN}Ports cleared${NC}\n"
        sleep 2
    else
        echo "Exiting. Run ./stop-services.sh first, then try again."
        exit 1
    fi
else
    echo -e "${GREEN}All ports are available${NC}\n"
fi

# Start ManufacturerService
echo "Starting ManufacturerService on port 5001..."
cd ManufacturerService
dotnet run > ../logs/manufacturer-service.log 2>&1 &
MANUFACTURER_PID=$!
cd ..
echo -e "${GREEN}ManufacturerService started (PID: $MANUFACTURER_PID)${NC}"

# Wait a bit for service to start
sleep 3

# Start DistributorService
echo "Starting DistributorService on port 5002..."
cd DistributorService
dotnet run > ../logs/distributor-service.log 2>&1 &
DISTRIBUTOR_PID=$!
cd ..
echo -e "${GREEN}DistributorService started (PID: $DISTRIBUTOR_PID)${NC}"

# Wait a bit for service to start
sleep 3

# Start SellerService
echo "Starting SellerService on port 5003..."
cd SellerService
dotnet run > ../logs/seller-service.log 2>&1 &
SELLER_PID=$!
cd ..
echo -e "${GREEN}SellerService started (PID: $SELLER_PID)${NC}"

# Wait for services to fully start
echo ""
echo "Waiting for services to initialize..."
sleep 5

# Create PID file for easy stopping
echo "$MANUFACTURER_PID" > .pids/manufacturer.pid
echo "$DISTRIBUTOR_PID" > .pids/distributor.pid
echo "$SELLER_PID" > .pids/seller.pid

echo ""
echo "=========================================="
echo -e "${GREEN}All services started successfully!${NC}"
echo "=========================================="
echo ""
echo "Services are running:"
echo "  - ManufacturerService: http://localhost:5001"
echo "  - DistributorService:  http://localhost:5002"
echo "  - SellerService:       http://localhost:5003"
echo ""
echo "Logs are being written to:"
echo "  - logs/manufacturer-service.log"
echo "  - logs/distributor-service.log"
echo "  - logs/seller-service.log"
echo ""
echo "To stop all services, run: ./stop-services.sh"
echo "Or press Ctrl+C and run: ./stop-services.sh"
echo ""

# Keep script running
wait
