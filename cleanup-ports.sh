#!/bin/bash

# Quick script to clean up ports 5001, 5002, 5003

echo "Cleaning up ports 5001, 5002, 5003..."

lsof -ti:5001 | xargs kill -9 2>/dev/null && echo "✓ Port 5001 cleared" || echo "  Port 5001 was free"
lsof -ti:5002 | xargs kill -9 2>/dev/null && echo "✓ Port 5002 cleared" || echo "  Port 5002 was free"
lsof -ti:5003 | xargs kill -9 2>/dev/null && echo "✓ Port 5003 cleared" || echo "  Port 5003 was free"

echo ""
echo "Done!"
