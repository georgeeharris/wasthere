# HTTPS/SSL Setup Guide for WasThere

This guide provides step-by-step instructions for enabling HTTPS on your WasThere deployment. There are two main approaches depending on your hosting setup.

## Overview

HTTPS (SSL/TLS) encrypts traffic between users and your server, providing:
- **Security**: Encrypted data transmission
- **Trust**: Browser security indicators (padlock icon)
- **SEO**: Better search engine rankings
- **Modern Features**: Required for many web APIs and features

## Prerequisites

- Domain name configured and pointing to your server (www.wasthere.co.uk)
- WasThere application deployed and running
- SSH access to your server
- Port 443 open in your firewall

## Choose Your Approach

### Option 1: Let's Encrypt with Certbot (Recommended for VPS/Self-Hosted)

**Best for**: Self-managed Linux VPS, AWS EC2, DigitalOcean, etc.

**Pros**:
- Free SSL certificates
- Automatic renewal
- Widely used and trusted
- Easy to set up

**Cons**:
- Requires shell access to server
- Need to set up auto-renewal

[Jump to Let's Encrypt Setup](#option-1-lets-encrypt-with-certbot-recommended)

### Option 2: IONOS SSL Certificate

**Best for**: IONOS hosting customers with managed SSL offerings

**Pros**:
- Integrated with IONOS control panel
- May include wildcard certificates
- IONOS support available

**Cons**:
- May have additional costs
- Configuration varies by IONOS product

[Jump to IONOS Setup](#option-2-ionos-ssl-certificate)

---

## Option 1: Let's Encrypt with Certbot (Recommended)

Let's Encrypt provides free, automated SSL certificates that are trusted by all major browsers.

### Step 1: Install Certbot

SSH into your server and install Certbot:

```bash
# For Ubuntu/Debian
sudo apt update
sudo apt install certbot python3-certbot-nginx -y

# For CentOS/RHEL/Fedora
sudo yum install certbot python3-certbot-nginx -y

# Verify installation
certbot --version
```

### Step 2: Stop WasThere Temporarily

Certbot needs to bind to port 80 temporarily to verify domain ownership:

```bash
cd ~/wasthere  # Or your application directory
docker compose down
```

### Step 3: Obtain SSL Certificate

Run Certbot in standalone mode to get your certificate:

```bash
# Replace www.wasthere.co.uk with your actual domain
sudo certbot certonly --standalone -d www.wasthere.co.uk

# Follow the prompts:
# - Enter your email address (for renewal notifications)
# - Agree to terms of service
# - Optionally share email with EFF
```

**Note**: If you also want to secure the API on a subdomain (e.g., api.wasthere.co.uk), add it:
```bash
sudo certbot certonly --standalone -d www.wasthere.co.uk -d api.wasthere.co.uk
```

### Step 4: Verify Certificate Location

Certbot stores certificates in `/etc/letsencrypt/live/your-domain/`:

```bash
sudo ls -la /etc/letsencrypt/live/www.wasthere.co.uk/

# You should see:
# - cert.pem        (Your domain certificate)
# - chain.pem       (Intermediate certificates)
# - fullchain.pem   (cert.pem + chain.pem)
# - privkey.pem     (Private key - keep secure!)
```

### Step 5: Update Docker Compose Configuration

**Note**: The `docker-compose.yml` already includes HTTPS support (port 443 and certificate volume mounts). If you cloned the latest version, you can skip modifying `docker-compose.yml`.

If needed, verify your `docker-compose.yml` has these settings:

```yaml
services:
  web:
    # ... existing configuration ...
    ports:
      - "${WEB_PORT:-80}:80"
      - "${WEB_HTTPS_PORT:-443}:443"  # HTTPS port
    volumes:
      # SSL Certificate volumes - already configured
      - /etc/letsencrypt:/etc/letsencrypt:ro
    # ... rest of configuration ...
```

### Step 6: Update nginx Configuration for HTTPS

The repository includes a ready-to-use HTTPS configuration. Activate it:

```bash
cd ~/wasthere  # Or your application directory

# Backup the current nginx config
cp nginx.conf nginx.conf.backup

# Use the HTTPS-enabled configuration
cp nginx-https-enabled.conf nginx.conf

# If using custom certificate paths (e.g., IONOS), edit nginx.conf:
nano nginx.conf
# Update lines 23-24 or uncomment lines 27-28 for custom paths
```

Alternatively, you can manually enable HTTPS in the existing `nginx.conf` by uncommenting the HTTPS server block (it has instructions in comments).

### Step 7: Configure Environment Variables

Edit your `.env` file to update API URL and CORS for HTTPS:

```bash
# Update these existing variables in .env
VITE_API_URL=https://www.wasthere.co.uk:5000/api
CORS_ORIGINS=https://www.wasthere.co.uk,http://www.wasthere.co.uk,https://www.wasthere.co.uk:5000,http://www.wasthere.co.uk:5000,http://localhost,http://localhost:5173,http://localhost:3000

# Optional: Add these for reference (not required for basic setup)
WEB_HTTPS_PORT=443
```

### Step 8: Start WasThere with HTTPS

### Step 8: Start WasThere with HTTPS

```bash
cd ~/wasthere  # Or your application directory

# Rebuild the web container to pick up new nginx config and .env changes
docker compose build web

# Start all services
docker compose up -d

# Check logs for any errors
docker compose logs -f web
```

### Step 9: Test HTTPS

1. Open your browser to https://www.wasthere.co.uk
2. Verify the padlock icon appears in the address bar
3. Click the padlock to view certificate details
4. Test that the application works correctly

### Step 10: Set Up Automatic Renewal

Let's Encrypt certificates expire after 90 days, but Certbot can auto-renew them.

Create a renewal script:

```bash
# Create renewal script
sudo nano /usr/local/bin/renew-wasthere-certs.sh
```

Add the following content:

```bash
#!/bin/bash
# WasThere SSL Certificate Renewal Script

set -e

echo "Stopping WasThere containers..."
cd /home/YOUR_USERNAME/wasthere  # Update this path
docker compose down

echo "Renewing certificates..."
certbot renew --standalone

echo "Starting WasThere containers..."
docker compose up -d

echo "Certificate renewal complete!"
docker compose logs --tail=20
```

Make it executable:

```bash
sudo chmod +x /usr/local/bin/renew-wasthere-certs.sh
```

Set up a cron job to run every 60 days:

```bash
sudo crontab -e

# Add this line (runs at 3 AM on the 1st of every other month)
0 3 1 */2 * /usr/local/bin/renew-wasthere-certs.sh >> /var/log/wasthere-cert-renewal.log 2>&1
```

**Or** use Certbot's timer (preferred on systemd-based systems):

```bash
# Check if timer is enabled
sudo systemctl status certbot.timer

# Enable if not already enabled
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer

# Note: With this approach, you'll need to restart containers manually
# or set up a post-renewal hook
```

To add a post-renewal hook:

```bash
sudo nano /etc/letsencrypt/renewal-hooks/post/restart-wasthere.sh
```

Add:

```bash
#!/bin/bash
cd /home/YOUR_USERNAME/wasthere  # Update this path
docker compose restart web
```

Make it executable:

```bash
sudo chmod +x /etc/letsencrypt/renewal-hooks/post/restart-wasthere.sh
```

### Step 10: Test Renewal (Optional)

Test the renewal process without actually renewing:

```bash
sudo certbot renew --dry-run
```

---

## Option 2: IONOS SSL Certificate

If you're using IONOS hosting with their managed SSL certificate service, follow these steps.

### Step 1: Purchase/Enable SSL Certificate via IONOS

1. Log into your [IONOS Control Panel](https://www.ionos.com/)
2. Navigate to **Domains & SSL**
3. Select your domain (www.wasthere.co.uk)
4. Look for SSL/TLS certificate options:
   - **SSL Certificate** or **SSL/TLS**
   - Choose between:
     - **Free DV (Domain Validated)** - If available
     - **Paid SSL** - For extended validation or wildcard

5. Follow IONOS's wizard to activate the certificate

### Step 2: Download Certificate Files from IONOS

Once activated, download your certificate files:

1. In IONOS Control Panel, go to **SSL Certificates**
2. Find your certificate for www.wasthere.co.uk
3. Download the certificate files:
   - **Certificate file** (usually `.crt` or `.pem`)
   - **Private key** (usually `.key` or `.pem`)
   - **Intermediate/Chain certificate** (if separate)

You should have these files:
- `www.wasthere.co.uk.crt` (or `.pem`)
- `www.wasthere.co.uk.key` (or `.pem`)
- `intermediate.crt` (or chain/bundle file)

### Step 3: Prepare Certificate Files on Server

SSH into your server and create a directory for certificates:

```bash
sudo mkdir -p /etc/ssl/wasthere
sudo chmod 700 /etc/ssl/wasthere
```

Upload your certificate files to the server:

```bash
# From your local machine, use scp (adjust paths):
scp www.wasthere.co.uk.crt YOUR_USER@YOUR_SERVER:/tmp/
scp www.wasthere.co.uk.key YOUR_USER@YOUR_SERVER:/tmp/
scp intermediate.crt YOUR_USER@YOUR_SERVER:/tmp/
```

Or create them directly on the server:

```bash
sudo nano /etc/ssl/wasthere/fullchain.pem
# Paste certificate content, then intermediate certificate content

sudo nano /etc/ssl/wasthere/privkey.pem
# Paste private key content
```

**Important**: If you have separate certificate and intermediate files, combine them:

```bash
# Combine into fullchain.pem
sudo cat /tmp/www.wasthere.co.uk.crt /tmp/intermediate.crt | sudo tee /etc/ssl/wasthere/fullchain.pem

# Move private key
sudo mv /tmp/www.wasthere.co.uk.key /etc/ssl/wasthere/privkey.pem

# Set proper permissions
sudo chmod 600 /etc/ssl/wasthere/privkey.pem
sudo chmod 644 /etc/ssl/wasthere/fullchain.pem

# Clean up
rm /tmp/*.crt /tmp/*.key
```

### Step 4: Configure Docker Compose and nginx

**Note**: The `docker-compose.yml` already includes HTTPS support. Verify your `docker-compose.yml` has:

```yaml
services:
  web:
    # ... existing configuration ...
    ports:
      - "${WEB_PORT:-80}:80"
      - "${WEB_HTTPS_PORT:-443}:443"
    volumes:
      # For IONOS/custom certificates:
      - /etc/ssl/wasthere:/etc/ssl/wasthere:ro
    # ... rest of configuration ...
```

Update nginx configuration for HTTPS:

```bash
cd ~/wasthere  # Or your application directory

# Backup the current nginx config
cp nginx.conf nginx.conf.backup

# Use the HTTPS-enabled configuration
cp nginx-https-enabled.conf nginx.conf

# Edit to use custom certificate paths
nano nginx.conf
# Uncomment lines 27-28 and comment out lines 23-24:
# ssl_certificate /etc/ssl/wasthere/fullchain.pem;
# ssl_certificate_key /etc/ssl/wasthere/privkey.pem;
```

### Step 5: Configure Environment Variables

Edit your `.env` file:

```bash
# Add these lines
WEB_HTTPS_PORT=443
ENABLE_HTTPS=true
SSL_CERT_PATH=/etc/ssl/wasthere/fullchain.pem
SSL_KEY_PATH=/etc/ssl/wasthere/privkey.pem

# Update these existing variables
VITE_API_URL=https://www.wasthere.co.uk:5000/api
CORS_ORIGINS=https://www.wasthere.co.uk,http://www.wasthere.co.uk,https://www.wasthere.co.uk:5000,http://www.wasthere.co.uk:5000,http://localhost,http://localhost:5173,http://localhost:3000
```

### Step 6: Restart WasThere

```bash
cd ~/wasthere  # Or your application directory

# Rebuild web container
docker compose build web

# Restart all services
docker compose up -d

# Check logs
docker compose logs -f web
```

### Step 7: Test HTTPS

1. Open https://www.wasthere.co.uk in your browser
2. Verify the padlock icon appears
3. Test the application functionality

### Step 8: Certificate Renewal (IONOS)

IONOS SSL certificates typically last 1 year. Set a reminder to renew:

1. **60 days before expiration**: Check IONOS Control Panel for renewal
2. **Download new certificate files** when ready
3. **Replace files** in `/etc/ssl/wasthere/`
4. **Restart web container**: `docker compose restart web`

Consider setting up a calendar reminder or monitoring service.

---

## Updating GitHub Actions for HTTPS (Both Options)

If you use GitHub Actions for deployment, update your secrets:

### Required GitHub Secrets

Go to: **Repository → Settings → Secrets and variables → Actions**

Update or add:

```
VITE_API_URL = https://www.wasthere.co.uk:5000/api
CORS_ORIGINS = https://www.wasthere.co.uk,http://www.wasthere.co.uk,https://www.wasthere.co.uk:5000,http://www.wasthere.co.uk:5000

# Add new secrets:
WEB_HTTPS_PORT = 443
ENABLE_HTTPS = true
SSL_CERT_PATH = /etc/letsencrypt/live/www.wasthere.co.uk/fullchain.pem
SSL_KEY_PATH = /etc/letsencrypt/live/www.wasthere.co.uk/privkey.pem
```

The deploy workflow will automatically use these values.

---

## Troubleshooting

### Certificate Not Found Error

**Symptom**: Nginx fails to start with "certificate not found" error

**Solution**:
1. Verify certificate paths in `.env`
2. Check file permissions: `sudo ls -la /etc/letsencrypt/live/www.wasthere.co.uk/`
3. Ensure Docker has access to mount the certificates

### Mixed Content Warnings

**Symptom**: Browser shows "mixed content" warnings

**Solution**:
1. Ensure `VITE_API_URL` uses `https://` in `.env`
2. Update all hardcoded URLs in source code to use HTTPS
3. Rebuild frontend: `docker compose build web`
4. Clear browser cache

### Certificate Expired

**Symptom**: Browser shows "certificate expired" error

**Solution**:
1. **Let's Encrypt**: Run `sudo certbot renew` manually
2. **IONOS**: Download and install new certificate from IONOS portal
3. Restart web container: `docker compose restart web`

### Port 443 Already in Use

**Symptom**: Cannot bind to port 443

**Solution**:
```bash
# Check what's using port 443
sudo netstat -tlnp | grep :443
# or
sudo lsof -i :443

# Stop conflicting service (e.g., Apache)
sudo systemctl stop apache2
sudo systemctl disable apache2
```

### Certbot Fails - Port 80 in Use

**Symptom**: Certbot cannot bind to port 80

**Solution**:
```bash
# Stop WasThere temporarily
docker compose down

# Run Certbot
sudo certbot certonly --standalone -d www.wasthere.co.uk

# Start WasThere
docker compose up -d
```

### API Still Using HTTP

**Symptom**: API returns errors when accessed via HTTPS on port 5000

**Note**: By default, the API continues to use HTTP on port 5000. To secure the API with HTTPS:

#### Option A: Reverse Proxy (Recommended)

Use nginx as a reverse proxy to handle SSL for both frontend and API:

1. Configure nginx to listen on 443 and proxy to API on port 5000
2. Keep API on HTTP internally (more efficient)
3. See "Advanced: Full HTTPS with Reverse Proxy" section below

#### Option B: Direct API SSL

Configure the .NET API to use HTTPS directly (requires more setup):

1. Configure Kestrel in `appsettings.json`
2. Mount certificates in API container
3. Update `ASPNETCORE_URLS` to include https

For most users, Option A (reverse proxy) is simpler and more maintainable.

---

## Advanced: Full HTTPS with Reverse Proxy

For a production-grade setup, use nginx as a reverse proxy for both frontend and API:

### Benefits:
- Single HTTPS endpoint for both frontend and API
- No CORS issues (same origin)
- Better performance (nginx SSL termination)
- Centralized SSL management

### Configuration:

This would require:
1. Updating nginx.conf to proxy `/api/` requests to the backend
2. Using a single domain/port for everything
3. Simplified CORS configuration

See `nginx-https-proxy.conf.example` (to be created) for a complete configuration.

---

## Security Best Practices

1. **Keep Certificates Secure**:
   - Never commit private keys to version control
   - Set proper file permissions (600 for private keys)
   - Limit access to certificate directories

2. **Use Strong SSL Configuration**:
   - The provided nginx config uses modern TLS protocols only
   - Disable older, insecure protocols (SSLv3, TLS 1.0, TLS 1.1)

3. **HTTP to HTTPS Redirect**:
   - The nginx configuration automatically redirects HTTP to HTTPS
   - Ensures all traffic is encrypted

4. **HSTS (HTTP Strict Transport Security)**:
   - Configured in nginx to force HTTPS for 1 year
   - Browsers will always use HTTPS after first visit

5. **Regular Updates**:
   - Keep certificates renewed (Let's Encrypt: 90 days, IONOS: typically 1 year)
   - Update nginx and Docker regularly for security patches

---

## Getting Help

### Let's Encrypt Issues
- [Let's Encrypt Community](https://community.letsencrypt.org/)
- [Certbot Documentation](https://certbot.eff.org/docs/)

### IONOS SSL Issues
- [IONOS Support](https://www.ionos.com/help/)
- IONOS SSL Documentation in your control panel

### WasThere Issues
- Check application logs: `docker compose logs -f`
- Review this guide's troubleshooting section
- Open an issue in the GitHub repository

---

## Summary

After completing this guide, you'll have:

- ✅ SSL/TLS certificate installed
- ✅ HTTPS enabled on port 443
- ✅ Automatic HTTP to HTTPS redirect
- ✅ Secure, encrypted traffic
- ✅ Browser security indicator (padlock)
- ✅ Certificate auto-renewal configured (Let's Encrypt)

Your WasThere application is now secure and ready for production use!
