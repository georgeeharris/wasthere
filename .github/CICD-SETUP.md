# CI/CD Pipeline Setup Guide

This document explains how to set up the GitHub Actions CI/CD pipeline for automatic deployment to your Linux VPS.

## Overview

The CI/CD pipeline consists of two workflows:

1. **CI Workflow** (`ci.yml`) - Runs on every push and pull request to test and build the application
2. **Deploy Workflow** (`deploy.yml`) - Automatically deploys to your VPS when changes are pushed to the `main` branch

## Prerequisites

Before setting up the CI/CD pipeline, ensure you have:

1. A Linux VPS with:
   - Docker and Docker Compose installed
   - Git installed
   - SSH access configured
   - The repository cloned in a directory on the VPS

2. SSH key pair for authentication (password-less authentication)

## Step 1: Prepare Your VPS

### 1.1 SSH into your VPS

```bash
ssh your_username@your_vps_ip
```

### 1.2 Install Docker (if not already installed)

```bash
# Clone or copy the repository to your VPS
cd ~
git clone https://github.com/georgeeharris/wasthere.git
cd wasthere

# Run the setup script
sudo bash setup-docker.sh

# Log out and back in for group permissions to take effect
exit
```

### 1.3 Set up the application directory

```bash
# SSH back in
ssh your_username@your_vps_ip

# Navigate to the application directory
cd ~/wasthere

# Make sure git is configured
git config pull.rebase false
```

## Step 2: Generate SSH Key for GitHub Actions

You need an SSH key that GitHub Actions will use to connect to your VPS.

### Option A: Generate a new SSH key (Recommended)

On your local machine:

```bash
# Generate a new SSH key specifically for GitHub Actions
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_deploy

# This creates two files:
# - github_actions_deploy (private key) - will be added to GitHub Secrets
# - github_actions_deploy.pub (public key) - will be added to VPS
```

### Option B: Use an existing SSH key

If you already have an SSH key that can access your VPS, you can use that instead.

### 2.1 Add the public key to your VPS

```bash
# Copy the public key to your clipboard
cat ~/.ssh/github_actions_deploy.pub

# SSH into your VPS
ssh your_username@your_vps_ip

# Add the public key to authorized_keys
echo "PASTE_PUBLIC_KEY_HERE" >> ~/.ssh/authorized_keys

# Ensure correct permissions
chmod 600 ~/.ssh/authorized_keys
chmod 700 ~/.ssh
```

## Step 3: Configure GitHub Secrets

GitHub Secrets are encrypted environment variables that you can use in your workflows. You need to add the following secrets to your GitHub repository.

### 3.1 Navigate to Repository Settings

1. Go to your repository on GitHub: `https://github.com/georgeeharris/wasthere`
2. Click on **Settings** (top menu)
3. In the left sidebar, click on **Secrets and variables** → **Actions**
4. Click on **New repository secret**

### 3.2 Add Required Secrets

Add each of the following secrets one by one:

#### **VPS_HOST** (Required)
- **Name:** `VPS_HOST`
- **Value:** Your VPS IP address or domain name
- **Example:** `192.168.1.100` or `wasthere.yourdomain.com`

#### **VPS_USERNAME** (Required)
- **Name:** `VPS_USERNAME`
- **Value:** The SSH username for your VPS
- **Example:** `ubuntu` or `root` or your custom username

#### **VPS_SSH_KEY** (Required)
- **Name:** `VPS_SSH_KEY`
- **Value:** The private SSH key content (entire file)
- **How to get it:**
  ```bash
  cat ~/.ssh/github_actions_deploy
  ```
- Copy the entire output including `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----END OPENSSH PRIVATE KEY-----`

#### **VPS_SSH_PORT** (Optional)
- **Name:** `VPS_SSH_PORT`
- **Value:** SSH port (default: `22`)
- Only add this if your VPS uses a non-standard SSH port

#### **VPS_APP_PATH** (Optional)
- **Name:** `VPS_APP_PATH`
- **Value:** The full path to the application directory on your VPS
- **Default:** `~/wasthere`
- **Example:** `/home/ubuntu/wasthere` or `/var/www/wasthere`

#### **POSTGRES_PASSWORD** (Required)
- **Name:** `POSTGRES_PASSWORD`
- **Value:** A strong password for your PostgreSQL database
- **Example:** Generate with: `openssl rand -base64 32`
- **Important:** Use a strong, unique password for production

#### **POSTGRES_DB** (Optional)
- **Name:** `POSTGRES_DB`
- **Value:** Database name
- **Default:** `wasthere`

#### **POSTGRES_USER** (Optional)
- **Name:** `POSTGRES_USER`
- **Value:** Database username
- **Default:** `postgres`

#### **VITE_API_URL** (Optional)
- **Name:** `VITE_API_URL`
- **Value:** The API URL that the frontend will use
- **Default:** `http://localhost:5000/api`
- **Production example:** `https://api.yourdomain.com/api` or `http://your-vps-ip:5000/api`

#### **WEB_PORT** (Optional)
- **Name:** `WEB_PORT`
- **Value:** Port for the web frontend
- **Default:** `80`
- Use `443` for HTTPS or another port if 80 is in use

#### **API_PORT** (Optional)
- **Name:** `API_PORT`
- **Value:** Port for the API
- **Default:** `5000`

#### **DB_PORT** (Optional)
- **Name:** `DB_PORT`
- **Value:** Port for PostgreSQL
- **Default:** `5432`

## Step 4: Test SSH Connection

Before deploying, verify that the SSH connection works from your local machine:

```bash
# Test SSH connection using the key
ssh -i ~/.ssh/github_actions_deploy your_username@your_vps_ip

# If it works, you should be logged in without a password
```

## Step 5: Enable GitHub Actions

1. Go to your repository on GitHub
2. Click on the **Actions** tab
3. If actions are disabled, click **"I understand my workflows, go ahead and enable them"**

## Step 6: Test the Pipeline

### Test CI Workflow

1. Create a new branch and make a small change
2. Push the branch and create a pull request
3. The CI workflow should automatically run and test your code
4. Check the **Actions** tab to see the workflow progress

### Test Deploy Workflow

1. Merge your changes to the `main` branch
2. The deploy workflow should automatically trigger
3. Check the **Actions** tab to monitor the deployment
4. SSH into your VPS to verify the deployment:

```bash
ssh your_username@your_vps_ip
cd ~/wasthere
docker compose ps
```

### Manual Deployment

You can also manually trigger a deployment:

1. Go to the **Actions** tab
2. Click on **Deploy to VPS** workflow
3. Click **Run workflow**
4. Select the `main` branch
5. Click **Run workflow**

## Step 7: Verify Deployment

After deployment, verify that your application is running:

1. **Check Docker containers:**
   ```bash
   ssh your_username@your_vps_ip
   cd ~/wasthere
   docker compose ps
   ```

2. **View logs:**
   ```bash
   docker compose logs -f
   ```

3. **Access the application:**
   - Frontend: `http://your_vps_ip` or `http://your_vps_ip:WEB_PORT`
   - API: `http://your_vps_ip:5000` or `http://your_vps_ip:API_PORT`
   - Swagger: `http://your_vps_ip:5000/swagger`

## Troubleshooting

### SSH Connection Issues

If the deployment fails with SSH connection errors:

1. **Verify the SSH key:**
   - Make sure you copied the entire private key including headers
   - Ensure there are no extra spaces or line breaks

2. **Check VPS SSH configuration:**
   ```bash
   # On your VPS, check if key-based auth is enabled
   sudo nano /etc/ssh/sshd_config
   # Ensure: PubkeyAuthentication yes
   sudo systemctl restart sshd
   ```

3. **Test SSH from your local machine:**
   ```bash
   ssh -i ~/.ssh/github_actions_deploy -v your_username@your_vps_ip
   ```

### Deployment Fails

If the deployment workflow fails:

1. **Check GitHub Actions logs:**
   - Go to Actions tab → Click on the failed workflow → View logs

2. **Common issues:**
   - **Git pull fails:** Make sure the repository is cloned on VPS
   - **Docker permission errors:** Ensure user is in docker group: `sudo usermod -aG docker $USER`
   - **Port already in use:** Stop existing services or change ports in secrets

### Database Issues

If the application can't connect to the database:

1. **Check if POSTGRES_PASSWORD is set:**
   ```bash
   ssh your_username@your_vps_ip
   cd ~/wasthere
   cat .env | grep POSTGRES_PASSWORD
   ```

2. **Restart containers:**
   ```bash
   docker compose down
   docker compose up -d
   ```

## Security Best Practices

1. **Use strong passwords:** Generate random passwords for `POSTGRES_PASSWORD`
2. **Limit SSH access:** Consider using a firewall to restrict SSH access
3. **Keep secrets secure:** Never commit secrets to your repository
4. **Regular updates:** Keep Docker and your VPS system updated
5. **Use HTTPS:** Set up a reverse proxy with SSL certificates for production
6. **Backup regularly:** Implement database backup strategy

## Workflow Details

### CI Workflow (`ci.yml`)

Runs on every push and pull request to `main` and `develop` branches:

1. **Test .NET API** - Restores dependencies, builds, and tests the API
2. **Test React Frontend** - Installs dependencies and builds the frontend
3. **Build Docker Images** - Builds both API and Web Docker images to verify they build successfully

### Deploy Workflow (`deploy.yml`)

Runs automatically on push to `main` branch or can be manually triggered:

1. Connects to VPS via SSH
2. Navigates to application directory
3. Pulls latest code from Git
4. Creates/updates `.env` file with secrets
5. Stops existing containers
6. Builds fresh Docker images
7. Starts containers
8. Shows status and logs
9. Cleans up unused Docker resources

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [WasThere Deployment Guide](DEPLOYMENT.md)

## Support

If you encounter issues:

1. Check the GitHub Actions logs in the **Actions** tab
2. Review the troubleshooting section above
3. Check the VPS logs: `docker compose logs -f`
4. Verify all secrets are correctly configured in GitHub
