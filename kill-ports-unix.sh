#!/bin/bash

# Nimbbl Sample App - Kill Ports Script (Unix/macOS/Linux)
# This script kills any processes running on ports 5000 and 5001

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Killing processes on ports 5000 and 5001...${NC}"

# Function to kill process on a port
kill_port() {
    local port=$1
    local pids=$(lsof -ti:$port 2>/dev/null)
    
    if [ -z "$pids" ]; then
        echo -e "${GREEN}✓ No process found on port $port${NC}"
        return 0
    fi
    
    echo -e "${YELLOW}Found process(es) on port $port: $pids${NC}"
    kill -9 $pids 2>/dev/null || true
    echo -e "${GREEN}✓ Killed process(es) on port $port${NC}"
}

# Kill processes on both ports
kill_port 5000
kill_port 5001

echo -e "${GREEN}Done!${NC}"

