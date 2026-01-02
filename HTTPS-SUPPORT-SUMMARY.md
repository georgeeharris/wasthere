# HTTPS Support - Implementation Summary

This document provides a quick reference for the HTTPS/SSL support that has been added to the WasThere application.

## What Was Added

### 1. Comprehensive Documentation
- **HTTPS-SETUP.md**: Complete guide with step-by-step instructions for:
  - Let's Encrypt with Certbot (recommended for VPS)
  - IONOS SSL certificates
  - Certificate installation and configuration
  - Automatic renewal setup
  - Troubleshooting

### 2. Configuration Files

#### nginx.conf (Default - HTTP with HTTPS Ready)
- Serves HTTP on port 80 by default
- Contains commented-out HTTPS configuration
- Easy to enable HTTPS by uncommenting the HTTPS server block
- Includes instructions in comments

#### nginx-https-enabled.conf (Ready-to-Use HTTPS Config)
- Complete HTTPS configuration with HTTP→HTTPS redirect
- Ready to use after obtaining SSL certificates
- Simply copy this file over nginx.conf after getting certificates
- Default paths: `/etc/letsencrypt/live/www.wasthere.co.uk/`
- Alternative paths for custom certificates (IONOS, etc.)

#### nginx-https-proxy.conf.example (Advanced Setup)
- Nginx as reverse proxy for both frontend and API
- Single HTTPS endpoint eliminates CORS issues
- Better security and performance
- For advanced users who want unified domain/port access

### 3. Docker Configuration Updates

#### docker-compose.yml
- Port 443 exposed for HTTPS
- Volume mounts configured for SSL certificates:
  - Let's Encrypt: `/etc/letsencrypt:/etc/letsencrypt:ro`
  - Custom certificates: `/etc/ssl/wasthere:/etc/ssl/wasthere:ro`
- Backward compatible - works without HTTPS

#### .env.example
- Added HTTPS configuration variables:
  - `WEB_HTTPS_PORT` (default: 443)
  - `ENABLE_HTTPS`
  - `SSL_CERT_PATH`
  - `SSL_KEY_PATH`
- Updated `VITE_API_URL` and `CORS_ORIGINS` examples to include HTTPS

### 4. GitHub Actions Deployment
- Updated `.github/workflows/deploy.yml` to support HTTPS secrets
- Optional HTTPS configuration variables
- Backward compatible with existing deployments

### 5. Documentation Updates
- **README.md**: Added HTTPS section with link to setup guide
- **DEPLOYMENT.md**: Added HTTPS configuration info and security notes
- **CORS-CONFIGURATION.md**: Updated with HTTPS considerations

## Quick Start Guide

### For New Deployments with HTTPS

1. **Deploy the application** (follow standard deployment)
2. **Obtain SSL certificate**:
   - **Let's Encrypt**: `sudo certbot certonly --standalone -d www.wasthere.co.uk`
   - **IONOS**: Download from IONOS control panel
3. **Enable HTTPS configuration**:
   ```bash
   cp nginx-https-enabled.conf nginx.conf
   # Edit nginx.conf if using custom certificate paths
   ```
4. **Update .env**:
   ```bash
   VITE_API_URL=https://www.wasthere.co.uk:5000/api
   CORS_ORIGINS=https://www.wasthere.co.uk,http://www.wasthere.co.uk,...
   ```
5. **Rebuild and restart**:
   ```bash
   docker compose build web
   docker compose up -d
   ```

### For Existing Deployments (Adding HTTPS)

1. **Pull latest changes**: `git pull`
2. **Follow HTTPS-SETUP.md** for your certificate provider
3. **Enable HTTPS** by copying `nginx-https-enabled.conf` to `nginx.conf`
4. **Update environment variables** in `.env`
5. **Rebuild and restart** containers

## Configuration Options

### Certificate Providers Supported

1. **Let's Encrypt** (Free, recommended)
   - Automatic renewal via Certbot
   - Trusted by all browsers
   - 90-day validity (auto-renewable)

2. **IONOS SSL** (Paid or included in hosting)
   - Integration with IONOS control panel
   - Typically 1-year validity
   - May include wildcard support

3. **Any SSL Provider**
   - As long as you have:
     - Certificate file (fullchain.pem)
     - Private key file (privkey.pem)

### Deployment Scenarios

1. **HTTP Only (Current Default)**
   - Use default `nginx.conf`
   - No certificate needed
   - Good for: Development, testing, initial setup

2. **HTTPS with Direct Access**
   - Use `nginx-https-enabled.conf`
   - Frontend: https://www.wasthere.co.uk
   - API: http://www.wasthere.co.uk:5000 (still HTTP)
   - Good for: Standard production deployments

3. **Full HTTPS with Reverse Proxy**
   - Use `nginx-https-proxy.conf.example`
   - Everything via HTTPS: https://www.wasthere.co.uk
   - API accessible via: https://www.wasthere.co.uk/api/
   - Good for: Maximum security, no CORS issues

## Security Considerations

### Implemented Security Features

1. **Modern TLS Protocols**: TLSv1.2 and TLSv1.3 only
2. **Strong Cipher Suites**: ECDHE, AES-GCM, ChaCha20-Poly1305
3. **HSTS Header**: Forces HTTPS for 1 year after first visit
4. **Security Headers**: X-Frame-Options, X-Content-Type-Options, etc.
5. **OCSP Stapling**: Improved SSL performance
6. **Automatic HTTP→HTTPS Redirect**: When enabled

### Recommended Production Settings

- ✅ Use HTTPS (port 443)
- ✅ Enable HSTS
- ✅ Use Let's Encrypt or trusted CA
- ✅ Set up automatic certificate renewal
- ✅ Update `CORS_ORIGINS` to include `https://` URLs
- ✅ Update `VITE_API_URL` to use `https://`
- ✅ Remove or restrict database port exposure
- ✅ Keep Docker and dependencies updated

## Files Modified/Added

### New Files
- `HTTPS-SETUP.md` - Complete HTTPS setup guide
- `nginx-https-enabled.conf` - Ready-to-use HTTPS nginx config
- `nginx-https-proxy.conf.example` - Advanced reverse proxy config
- `HTTPS-SUPPORT-SUMMARY.md` - This file

### Modified Files
- `nginx.conf` - Updated with commented HTTPS config
- `docker-compose.yml` - Added port 443 and certificate volumes
- `.env.example` - Added HTTPS configuration options
- `.github/workflows/deploy.yml` - Added HTTPS secrets support
- `README.md` - Added HTTPS section
- `DEPLOYMENT.md` - Added HTTPS configuration info
- `CORS-CONFIGURATION.md` - Added HTTPS considerations

## Troubleshooting Quick Reference

### Certificate Not Found
- Verify certificate paths in nginx.conf
- Check file permissions: `sudo ls -la /etc/letsencrypt/live/www.wasthere.co.uk/`
- Ensure Docker can access mounted volumes

### Mixed Content Warnings
- Update `VITE_API_URL` to use `https://` in `.env`
- Rebuild frontend: `docker compose build web`
- Clear browser cache

### Port 443 Already in Use
```bash
sudo netstat -tlnp | grep :443
sudo systemctl stop apache2  # If Apache is running
```

### Certificate Expired
- Let's Encrypt: `sudo certbot renew`
- IONOS: Download new certificate from control panel
- Restart: `docker compose restart web`

## Next Steps

1. **Review HTTPS-SETUP.md** for detailed implementation steps
2. **Choose your certificate provider** (Let's Encrypt or IONOS)
3. **Follow the setup guide** for your chosen provider
4. **Test thoroughly** after enabling HTTPS
5. **Set up automatic renewal** (critical for Let's Encrypt)

## Support

For detailed instructions, see:
- [HTTPS Setup Guide](HTTPS-SETUP.md)
- [Deployment Guide](DEPLOYMENT.md)
- [CORS Configuration](CORS-CONFIGURATION.md)

For issues specific to:
- **Let's Encrypt**: https://community.letsencrypt.org/
- **IONOS**: https://www.ionos.com/help/
- **WasThere App**: Open an issue in the GitHub repository

## Backward Compatibility

All changes are **backward compatible**:
- Existing HTTP deployments continue to work
- HTTPS is **opt-in**, not required
- Default configuration remains HTTP
- No breaking changes to existing deployments

You can enable HTTPS at any time by following the setup guide.
