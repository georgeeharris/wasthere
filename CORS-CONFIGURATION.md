# CORS Configuration Guide

## Overview

This document explains the Cross-Origin Resource Sharing (CORS) configuration for the WasThere application after the domain configuration change to www.wasthere.co.uk.

## Architecture

The WasThere application consists of two main components:
- **Web UI**: React application served by Nginx on port 80/443
- **API**: ASP.NET Core Web API on port 5000

## CORS Requirements

When the Web UI and API run on different ports, the browser treats them as different origins. For the Web UI to successfully make API calls, the API must explicitly allow the Web UI's origin in its CORS configuration.

### Origin Examples

| Component | URL | Port | Protocol |
|-----------|-----|------|----------|
| Web UI (HTTP) | http://www.wasthere.co.uk | 80 | HTTP |
| Web UI (HTTPS) | https://www.wasthere.co.uk | 443 | HTTPS |
| API (HTTP) | http://www.wasthere.co.uk:5000 | 5000 | HTTP |
| API (HTTPS) | https://www.wasthere.co.uk:5000 | 5000 | HTTPS |

## Configuration Files

### 1. appsettings.json

The default CORS configuration for the API is in `WasThere.Api/appsettings.json`:

```json
{
  "CorsOrigins": "http://www.wasthere.co.uk,https://www.wasthere.co.uk,http://www.wasthere.co.uk:5000,https://www.wasthere.co.uk:5000,http://localhost:5173,http://localhost:3000,http://localhost,http://localhost:5000"
}
```

### 2. Environment Variables (.env)

For Docker deployments, the CORS configuration can be overridden using the `CORS_ORIGINS` environment variable:

```bash
CORS_ORIGINS=http://www.wasthere.co.uk,https://www.wasthere.co.uk,http://www.wasthere.co.uk:5000,https://www.wasthere.co.uk:5000,http://localhost,http://localhost:5173,http://localhost:3000,http://localhost:5000
```

Copy `.env.example` to `.env` and customize as needed.

### 3. docker-compose.yml

The API service reads the `CORS_ORIGINS` environment variable:

```yaml
api:
  environment:
    - CorsOrigins=${CORS_ORIGINS:-http://localhost,http://localhost:5173,http://localhost:3000}
```

## Why Include API Port in CORS?

The CORS configuration includes the API URLs with port numbers (e.g., `http://www.wasthere.co.uk:5000`) for the following reasons:

1. **Swagger UI Access**: When accessing the Swagger documentation directly at the API URL, it makes AJAX requests that need CORS approval
2. **Development Tools**: Browser-based API testing tools and development consoles may access the API directly
3. **Future Extensibility**: Allows for potential API-to-API communication scenarios

## Development vs Production

### Development (localhost)

The configuration includes multiple localhost ports for development:
- `http://localhost` - Standard HTTP
- `http://localhost:5173` - Vite dev server default port
- `http://localhost:3000` - Common React dev server port
- `http://localhost:5000` - API port

### Production (www.wasthere.co.uk)

The configuration includes both HTTP and HTTPS for production:
- `http://www.wasthere.co.uk` - Web UI (HTTP)
- `https://www.wasthere.co.uk` - Web UI (HTTPS)
- `http://www.wasthere.co.uk:5000` - API (HTTP)
- `https://www.wasthere.co.uk:5000` - API (HTTPS)

## Troubleshooting

### Symptom: UI fails to load or shows network errors

**Possible Causes:**
1. CORS origins not configured correctly
2. API not running
3. Firewall blocking the API port

**Solutions:**
1. Check browser console for CORS errors
2. Verify the `.env` file has the correct `CORS_ORIGINS` value
3. Restart the API container: `docker-compose restart api`
4. Verify API is accessible: `curl http://www.wasthere.co.uk:5000/api/events`

### Symptom: CORS errors in browser console

**Example Error:**
```
Access to fetch at 'http://www.wasthere.co.uk:5000/api/events' from origin 'http://www.wasthere.co.uk' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

**Solution:**
1. Ensure the Web UI origin (without port number) is in the CORS configuration
2. Check that the API is using the environment variable correctly
3. Verify the API logs show the correct CORS origins on startup

### Debugging CORS

To test CORS configuration, use curl to simulate a browser preflight request:

```bash
# Test from Web UI origin
curl -X OPTIONS http://www.wasthere.co.uk:5000/api/events \
  -H "Origin: http://www.wasthere.co.uk" \
  -H "Access-Control-Request-Method: GET" \
  -v

# Should return:
# Access-Control-Allow-Origin: http://www.wasthere.co.uk
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE, etc.
```

## Security Considerations

1. **Production**: Only include the actual production domains in CORS origins
2. **Remove localhost origins**: Remove all `localhost` entries from production `.env`
3. **HTTPS**: Use HTTPS in production for security
4. **Wildcards**: Avoid using wildcards (`*`) in CORS origins for production

## References

- [MDN: CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [ASP.NET Core CORS Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
