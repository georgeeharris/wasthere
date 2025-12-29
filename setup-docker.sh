#!/bin/bash

# setup-docker.sh
# Script to install Docker and Docker Compose on a fresh Linux environment
# Supports Ubuntu/Debian and CentOS/RHEL distributions

set -e

echo "==================================="
echo "Docker Installation Script"
echo "==================================="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

# Detect OS
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
    VERSION=$VERSION_ID
else
    echo "Cannot detect OS. This script supports Ubuntu/Debian and CentOS/RHEL."
    exit 1
fi

echo "Detected OS: $OS $VERSION"
echo ""

# Install Docker based on OS
case $OS in
    ubuntu|debian)
        echo "Installing Docker on Ubuntu/Debian..."
        
        # Update package index
        apt-get update
        
        # Install prerequisites
        apt-get install -y \
            ca-certificates \
            curl \
            gnupg \
            lsb-release
        
        # Add Docker's official GPG key
        install -m 0755 -d /etc/apt/keyrings
        curl -fsSL https://download.docker.com/linux/$OS/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
        chmod a+r /etc/apt/keyrings/docker.gpg
        
        # Set up the repository
        echo \
          "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$OS \
          $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
        
        # Install Docker Engine
        apt-get update
        apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        ;;
        
    centos|rhel)
        echo "Installing Docker on CentOS/RHEL..."
        
        # Detect version to use appropriate package manager
        if command -v dnf &> /dev/null; then
            # RHEL 8+ or CentOS 8+ uses dnf
            PKG_MGR="dnf"
            dnf install -y dnf-plugins-core
            dnf config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
        else
            # Older versions use yum
            PKG_MGR="yum"
            yum install -y yum-utils
            yum-config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
        fi
        
        # Install Docker Engine
        $PKG_MGR install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        
        # Start Docker
        systemctl start docker
        ;;
    
    fedora)
        echo "Installing Docker on Fedora..."
        
        # Install prerequisites
        dnf install -y dnf-plugins-core
        
        # Set up the repository
        dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo
        
        # Install Docker Engine
        dnf install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        
        # Start Docker
        systemctl start docker
        ;;
        
    *)
        echo "Unsupported OS: $OS"
        echo "This script supports Ubuntu, Debian, CentOS, RHEL, and Fedora."
        exit 1
        ;;
esac

# Enable Docker to start on boot
systemctl enable docker

# Verify Docker installation
echo ""
echo "Verifying Docker installation..."
docker --version
docker compose version

# Add current user to docker group if not root
if [ -n "$SUDO_USER" ]; then
    echo ""
    echo "Adding user $SUDO_USER to docker group..."
    usermod -aG docker $SUDO_USER
    echo "Note: $SUDO_USER will need to log out and back in for group changes to take effect."
fi

echo ""
echo "==================================="
echo "Docker installation completed!"
echo "==================================="
echo ""
echo "To verify the installation, run:"
echo "  docker run hello-world"
echo ""
echo "To start the WasThere application:"
echo "  1. Navigate to the application directory"
echo "  2. Run: docker compose up -d"
echo "  3. Access the app at http://localhost"
echo ""
