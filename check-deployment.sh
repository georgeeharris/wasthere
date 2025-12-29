#!/bin/bash

# check-deployment.sh
# Script to check if the deployment is properly configured for production

set -e

echo "========================================"
echo "WasThere Deployment Configuration Check"
echo "========================================"
echo ""

ERRORS=0
WARNINGS=0

# Check if .env file exists
if [ ! -f .env ]; then
    echo "❌ ERROR: .env file not found"
    echo "   Run: cp .env.example .env"
    echo "   Then edit .env to set secure credentials"
    ERRORS=$((ERRORS + 1))
else
    echo "✓ .env file exists"
    
    # Check if password is set
    if grep -q "POSTGRES_PASSWORD=change_this_password_in_production" .env 2>/dev/null; then
        echo "❌ ERROR: Default password detected in .env"
        echo "   Please set a strong POSTGRES_PASSWORD in .env"
        ERRORS=$((ERRORS + 1))
    elif grep -q "POSTGRES_PASSWORD=" .env 2>/dev/null; then
        PASSWORD=$(grep "POSTGRES_PASSWORD=" .env | cut -d'=' -f2)
        PASSWORD_LENGTH=${#PASSWORD}
        
        if [ $PASSWORD_LENGTH -lt 12 ]; then
            echo "⚠️  WARNING: Password is less than 12 characters"
            echo "   Consider using a longer password for better security"
            WARNINGS=$((WARNINGS + 1))
        else
            echo "✓ Password is set and meets minimum length"
        fi
    else
        echo "❌ ERROR: POSTGRES_PASSWORD not found in .env"
        ERRORS=$((ERRORS + 1))
    fi
fi

# Check if Docker is installed
if command -v docker &> /dev/null; then
    echo "✓ Docker is installed ($(docker --version))"
else
    echo "❌ ERROR: Docker is not installed"
    echo "   Run: sudo bash setup-docker.sh"
    ERRORS=$((ERRORS + 1))
fi

# Check if Docker Compose is installed
if command -v docker compose &> /dev/null; then
    echo "✓ Docker Compose is installed ($(docker compose version))"
else
    echo "❌ ERROR: Docker Compose is not installed"
    echo "   Run: sudo bash setup-docker.sh"
    ERRORS=$((ERRORS + 1))
fi

# Check if docker-compose.yml exists
if [ -f docker-compose.yml ]; then
    echo "✓ docker-compose.yml exists"
    
    # Validate docker-compose syntax if Docker is installed
    if command -v docker compose &> /dev/null; then
        if [ -f .env ]; then
            if docker compose config --quiet 2>/dev/null; then
                echo "✓ docker-compose.yml syntax is valid"
            else
                echo "❌ ERROR: docker-compose.yml has syntax errors"
                ERRORS=$((ERRORS + 1))
            fi
        fi
    fi
else
    echo "❌ ERROR: docker-compose.yml not found"
    ERRORS=$((ERRORS + 1))
fi

# Check required Dockerfiles
if [ -f Dockerfile.api ]; then
    echo "✓ Dockerfile.api exists"
else
    echo "❌ ERROR: Dockerfile.api not found"
    ERRORS=$((ERRORS + 1))
fi

if [ -f Dockerfile.web ]; then
    echo "✓ Dockerfile.web exists"
else
    echo "❌ ERROR: Dockerfile.web not found"
    ERRORS=$((ERRORS + 1))
fi

# Check port availability (if Docker is installed)
if command -v docker &> /dev/null; then
    if [ -f .env ]; then
        source .env
        WEB_PORT=${WEB_PORT:-80}
        API_PORT=${API_PORT:-5000}
        DB_PORT=${DB_PORT:-5432}
        
        # Check ports
        for PORT in $WEB_PORT $API_PORT $DB_PORT; do
            if lsof -Pi :$PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
                echo "⚠️  WARNING: Port $PORT is already in use"
                echo "   Either stop the service using this port or change the port in .env"
                WARNINGS=$((WARNINGS + 1))
            else
                echo "✓ Port $PORT is available"
            fi
        done
    fi
fi

# Check disk space
AVAILABLE_SPACE=$(df -BG . | tail -1 | awk '{print $4}' | sed 's/G//')
if [ $AVAILABLE_SPACE -lt 5 ]; then
    echo "⚠️  WARNING: Less than 5GB disk space available"
    echo "   Recommended: At least 10GB free space"
    WARNINGS=$((WARNINGS + 1))
else
    echo "✓ Sufficient disk space available (${AVAILABLE_SPACE}GB)"
fi

# Summary
echo ""
echo "========================================"
echo "Summary:"
echo "  Errors: $ERRORS"
echo "  Warnings: $WARNINGS"
echo "========================================"
echo ""

if [ $ERRORS -gt 0 ]; then
    echo "❌ Deployment is NOT ready. Please fix the errors above."
    exit 1
elif [ $WARNINGS -gt 0 ]; then
    echo "⚠️  Deployment has warnings. Consider addressing them for production."
    exit 0
else
    echo "✅ Deployment is ready!"
    echo ""
    echo "To start the application, run:"
    echo "  docker compose up -d"
    exit 0
fi
