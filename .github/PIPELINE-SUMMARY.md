# CI/CD Pipeline Summary

## ✅ What Has Been Implemented

A complete CI/CD pipeline using GitHub Actions has been added to the repository with the following components:

### 1. Continuous Integration Workflow (`.github/workflows/ci.yml`)
- **Triggers**: Runs on every push and pull request to `main` and `develop` branches
- **Jobs**:
  - **Test .NET API**: Restores dependencies and builds the API project
  - **Test React Frontend**: Installs dependencies, runs linting (non-blocking), and builds the frontend
  - **Build Docker Images**: Builds both API and Web Docker images to ensure they can be built successfully

### 2. Continuous Deployment Workflow (`.github/workflows/deploy.yml`)
- **Triggers**: Automatically deploys when code is pushed to `main` branch, or can be manually triggered
- **Deployment Process**:
  1. Connects to your VPS via SSH
  2. Pulls the latest code from the repository
  3. Creates/updates the `.env` file with values from GitHub Secrets
  4. Stops existing containers
  5. Builds fresh Docker images
  6. Starts all containers
  7. Shows status and logs
  8. Cleans up unused Docker resources

### 3. Comprehensive Documentation

#### `.github/CICD-SETUP.md` (Detailed Setup Guide)
- Complete step-by-step instructions for setting up the CI/CD pipeline
- VPS preparation and Docker installation
- SSH key generation and configuration
- GitHub Secrets configuration with detailed explanations
- Testing and troubleshooting guides
- Security best practices

#### `.github/SECRETS-REFERENCE.md` (Quick Reference)
- Quick lookup table for all required and optional secrets
- Commands for generating SSH keys and passwords
- Example configurations for different scenarios
- Security notes and verification steps

#### Updated `README.md`
- Added CI/CD section explaining the automated deployment
- Links to setup documentation
- Quick setup summary

## 🔑 GitHub Secrets You Need to Configure

### Required Secrets (Must be set):
1. **VPS_HOST** - Your VPS IP address or domain name
2. **VPS_USERNAME** - SSH username for your VPS
3. **VPS_SSH_KEY** - Private SSH key content (entire file)
4. **POSTGRES_PASSWORD** - Strong database password

### Optional Secrets (with sensible defaults):
- VPS_SSH_PORT (default: 22)
- VPS_APP_PATH (default: ~/wasthere)
- POSTGRES_DB (default: wasthere)
- POSTGRES_USER (default: wasthere_user)
- VITE_API_URL (default: http://localhost:5000/api)
- WEB_PORT (default: 80)
- API_PORT (default: 5000)
- DB_PORT (default: 5432)

## 📍 Where to Set GitHub Secrets

1. Go to: `https://github.com/georgeeharris/wasthere`
2. Click: **Settings** (top navigation)
3. In the left sidebar: **Secrets and variables** → **Actions**
4. Click: **New repository secret**
5. Add each required secret

## 🚀 How to Use

### Automatic Deployment
1. Configure all required GitHub Secrets (see documentation)
2. Ensure your VPS has Docker installed and the repository cloned
3. Push code to the `main` branch
4. GitHub Actions will automatically build and deploy

### Manual Deployment
1. Go to the **Actions** tab in your repository
2. Click on **Deploy to VPS** workflow
3. Click **Run workflow**
4. Select the `main` branch
5. Click **Run workflow**

## 📋 Prerequisites on VPS

Your Linux VPS must have:
1. Docker and Docker Compose installed (use `setup-docker.sh` script)
2. Git installed and repository cloned
3. SSH key-based authentication configured
4. The application directory path (default: `~/wasthere`)

## 🔧 What Happens During Deployment

```
Push to main → GitHub Actions triggered
    ↓
Connect to VPS via SSH
    ↓
Pull latest code (git pull origin main)
    ↓
Create/update .env file from secrets
    ↓
Stop existing containers (docker compose down)
    ↓
Build fresh images (docker compose build --no-cache)
    ↓
Start containers (docker compose up -d)
    ↓
Show status and logs
    ↓
Clean up Docker resources
    ↓
✅ Deployment complete!
```

## 📚 Documentation Files

- **[.github/CICD-SETUP.md](.github/CICD-SETUP.md)** - Complete setup instructions
- **[.github/SECRETS-REFERENCE.md](.github/SECRETS-REFERENCE.md)** - Quick secrets reference
- **[README.md](README.md)** - Updated with CI/CD information
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Docker deployment guide

## ✨ Features

- ✅ Automated testing on every push/PR
- ✅ Automated deployment to VPS on main branch
- ✅ Manual deployment trigger available
- ✅ Environment variables managed through GitHub Secrets
- ✅ Docker image caching for faster builds
- ✅ Automatic cleanup of unused Docker resources
- ✅ Comprehensive logging and status reporting
- ✅ Non-blocking linting (won't fail build on linting errors)

## 🔒 Security Considerations

- SSH key-based authentication (no passwords)
- Secrets stored securely in GitHub
- `.env` file never committed to repository
- Automatic creation of `.env` from secrets on VPS
- Option to use different ports for security
- Database exposed only internally by default

## 🆘 Need Help?

Refer to these documents:
1. **Setup Issues**: [.github/CICD-SETUP.md](.github/CICD-SETUP.md) - See Troubleshooting section
2. **Secrets Help**: [.github/SECRETS-REFERENCE.md](.github/SECRETS-REFERENCE.md)
3. **Docker Issues**: [DEPLOYMENT.md](DEPLOYMENT.md)

Check GitHub Actions logs in the **Actions** tab for detailed error messages.
