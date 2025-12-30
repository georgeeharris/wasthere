# GitHub Secrets Quick Reference

This document provides a quick reference for all GitHub Secrets needed for the CI/CD pipeline.

## Where to Add Secrets

1. Go to: `https://github.com/georgeeharris/wasthere`
2. Click: **Settings** → **Secrets and variables** → **Actions**
3. Click: **New repository secret**

## Required Secrets

| Secret Name | Description | Example |
|------------|-------------|---------|
| `VPS_HOST` | Your VPS IP address or domain | `192.168.1.100` |
| `VPS_USERNAME` | SSH username for VPS | `ubuntu` |
| `VPS_SSH_KEY` | Private SSH key (full content) | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `POSTGRES_PASSWORD` | Database password (use strong password!) | Generate: `openssl rand -base64 32` |

## Optional Secrets (with defaults)

| Secret Name | Description | Default |
|------------|-------------|---------|
| `VPS_SSH_PORT` | SSH port | `22` |
| `VPS_APP_PATH` | App directory on VPS | `~/wasthere` |
| `POSTGRES_DB` | Database name | `wasthere` |
| `POSTGRES_USER` | Database username | `postgres` |
| `CORS_ORIGINS` | Comma-separated list of allowed CORS origins | `http://localhost,http://localhost:5173,http://localhost:3000` |
| `VITE_API_URL` | Frontend API URL | `http://localhost:5000/api` |
| `WEB_PORT` | Web frontend port | `80` |
| `API_PORT` | API port | `5000` |
| `DB_PORT` | Database port | `5432` |

## Generating Values

### SSH Key Pair
```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_deploy
# Use the private key (github_actions_deploy) for VPS_SSH_KEY secret
# Add the public key (github_actions_deploy.pub) to VPS ~/.ssh/authorized_keys
```

### Strong Database Password
```bash
openssl rand -base64 32
```

### Get Private Key Content
```bash
cat ~/.ssh/github_actions_deploy
# Copy entire output for VPS_SSH_KEY secret
```

## Common Configurations

### Development/Testing Setup
```
VPS_HOST: your-vps-ip
VPS_USERNAME: ubuntu
VPS_SSH_KEY: <your-private-key>
POSTGRES_PASSWORD: <strong-password>
```

### Production Setup with Custom Domain
```
VPS_HOST: wasthere.yourdomain.com
VPS_USERNAME: ubuntu
VPS_SSH_KEY: <your-private-key>
POSTGRES_PASSWORD: <strong-password>
CORS_ORIGINS: http://yourdomain.com,https://yourdomain.com
VITE_API_URL: https://api.yourdomain.com/api
WEB_PORT: 443
```

**Important for Production:**
- **Always set `CORS_ORIGINS`** to your actual domain(s) to prevent CORS errors
- For production with HTTPS, use only HTTPS origins for better security: `CORS_ORIGINS: https://yourdomain.com`
- The default localhost values are only suitable for development environments

## Verifying Secrets

After adding secrets, verify them:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. You should see all secret names listed (values are hidden)
3. Update or delete secrets as needed

## Security Notes

⚠️ **Important:**
- Never commit secrets to your repository
- Use strong, randomly generated passwords
- Rotate secrets regularly
- Limit access to repository settings
- Review GitHub Actions logs for sensitive data exposure

## Next Steps

After configuring secrets:
1. Push code to `main` branch to trigger deployment
2. Monitor deployment in **Actions** tab
3. Verify application is running on VPS

For detailed setup instructions, see [CICD-SETUP.md](CICD-SETUP.md)
