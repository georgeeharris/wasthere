# HTTPS Quick Start Guide

This is a condensed version of the HTTPS setup. For detailed instructions, see [HTTPS-SETUP.md](HTTPS-SETUP.md).

## Overview

HTTPS support has been added to WasThere! You can now secure your site with SSL/TLS encryption.

## Two Approaches

### Option 1: Let's Encrypt (Free - Recommended)
Best for self-managed VPS/servers. Free, automated, and trusted.

### Option 2: IONOS SSL
Best if you're using IONOS hosting with their SSL service.

## Quick Setup Steps

### 1. Stop WasThere
```bash
cd ~/wasthere
docker compose down
```

### 2. Get SSL Certificate

**For Let's Encrypt:**
```bash
sudo apt install certbot -y
sudo certbot certonly --standalone -d www.wasthere.co.uk
```

**For IONOS:**
- Download certificate from IONOS control panel
- Place in `/etc/ssl/wasthere/`

### 3. Enable HTTPS Configuration
```bash
cd ~/wasthere
cp nginx-https-enabled.conf nginx.conf

# If using IONOS certificates, edit nginx.conf:
# Uncomment lines for custom certificate paths
```

### 4. Update Environment Variables
Edit `.env`:
```bash
VITE_API_URL=https://www.wasthere.co.uk:5000/api
CORS_ORIGINS=https://www.wasthere.co.uk,http://www.wasthere.co.uk,https://www.wasthere.co.uk:5000,http://www.wasthere.co.uk:5000
```

### 5. Restart WasThere
```bash
docker compose build web
docker compose up -d
```

### 6. Test
Open https://www.wasthere.co.uk in your browser. You should see a padlock icon! 🔒

## What If I Don't Want HTTPS?

No problem! The default configuration works without HTTPS. Simply skip this guide and continue using HTTP.

## Files You Need to Know

- **HTTPS-SETUP.md** - Complete detailed guide (read this!)
- **nginx-https-enabled.conf** - Ready-to-use HTTPS config
- **nginx.conf** - Current config (HTTP by default, HTTPS ready)
- **HTTPS-SUPPORT-SUMMARY.md** - Technical summary

## Need Help?

See [HTTPS-SETUP.md](HTTPS-SETUP.md) for:
- Detailed step-by-step instructions
- Troubleshooting guide
- Certificate renewal setup
- Security best practices

## Can I Do This Through IONOS Tooling?

If you're using IONOS hosting, you have two options:

1. **IONOS SSL Service** (Recommended for IONOS users)
   - Purchase/activate SSL through IONOS control panel
   - Download certificate files
   - Follow the IONOS section in [HTTPS-SETUP.md](HTTPS-SETUP.md)

2. **Let's Encrypt on Your Server** (If you have shell access)
   - Use certbot as described above
   - Follow the Let's Encrypt section in [HTTPS-SETUP.md](HTTPS-SETUP.md)

**Note**: Some IONOS managed services may handle SSL automatically through their control panel. Check your IONOS documentation or support for specific instructions for your hosting plan.

## Source Code Changes

The HTTPS support is implemented through:
- **Configuration files** (nginx, docker-compose, .env)
- **No application code changes needed**
- **Backward compatible** - works with or without HTTPS

You don't need to modify any source code. Just follow the configuration steps above!
