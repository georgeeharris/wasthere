# Google Gemini API Key Deployment Configuration

## Overview
This document describes how the Google Gemini API key is configured and deployed to production.

## Configuration Flow

### 1. GitHub Secrets (Production)
The API key is stored as a GitHub Secret and automatically deployed:

```
GitHub Secret: GOOGLE_GEMINI_API_KEY
    ↓ (via deploy.yml workflow)
.env file on VPS: GOOGLE_GEMINI_API_KEY=<your-key>
    ↓ (via docker-compose.yml)
Container Environment: GoogleGemini__ApiKey=<your-key>
    ↓ (via ASP.NET Configuration)
Application reads: configuration["GoogleGemini:ApiKey"]
```

### 2. Local Development
For local development, the API key can be set in multiple ways:

**Option A: appsettings.json (Default for testing)**
```json
{
  "GoogleGemini": {
    "ApiKey": "AIzaSyBWz6nv-fjmX3vpzxgmO1rTA8I9G0JFhMA"
  }
}
```

**Option B: Environment Variable**
```bash
# Linux/Mac
export GoogleGemini__ApiKey="your-api-key"
dotnet run

# Windows
set GoogleGemini__ApiKey=your-api-key
dotnet run
```

**Option C: User Secrets (Recommended for development)**
```bash
cd WasThere.Api
dotnet user-secrets init
dotnet user-secrets set "GoogleGemini:ApiKey" "your-api-key"
```

## Setup Instructions for Production

### Step 1: Add GitHub Secret
1. Go to: https://github.com/georgeeharris/wasthere/settings/secrets/actions
2. Click: **New repository secret**
3. Name: `GOOGLE_GEMINI_API_KEY`
4. Value: Your Google Gemini API key
5. Click: **Add secret**

### Step 2: Deploy
The API key will be automatically included in the next deployment:
```bash
# Deployment happens automatically on push to main
git push origin main

# Or trigger manually from GitHub Actions tab
```

### Step 3: Verify
After deployment, verify the API key is configured:
```bash
# SSH to your VPS
ssh username@your-vps

# Check .env file (key should be present)
cd ~/wasthere
grep GOOGLE_GEMINI_API_KEY .env

# Check container environment (key should be set)
docker exec wasthere-api env | grep GoogleGemini

# Check API logs for any configuration errors
docker logs wasthere-api | grep -i gemini
```

## How It Works in Deployment

### deploy.yml Workflow
```yaml
echo "GOOGLE_GEMINI_API_KEY=${{ secrets.GOOGLE_GEMINI_API_KEY }}" >> .env
```
This line adds the API key to the `.env` file on the VPS during every deployment.

### docker-compose.yml
```yaml
api:
  environment:
    - GoogleGemini__ApiKey=${GOOGLE_GEMINI_API_KEY}
```
Docker Compose reads the key from `.env` and passes it to the container.

**Note:** The double underscore `__` in `GoogleGemini__ApiKey` is ASP.NET's convention for nested configuration. It translates to:
```json
{
  "GoogleGemini": {
    "ApiKey": "value"
  }
}
```

## Security Considerations

### ✅ Good Practices
- API key stored in GitHub Secrets (encrypted at rest)
- Not committed to repository
- Different keys can be used for dev/staging/prod
- Can be rotated without code changes

### ⚠️ Important Notes
- The test key in `appsettings.json` is for local development only
- Production should **always** use the GitHub Secret
- Never commit actual API keys to the repository
- Rotate keys regularly

## Troubleshooting

### API Key Not Working in Production
1. **Check GitHub Secret exists:**
   - Go to: Settings → Secrets and variables → Actions
   - Verify `GOOGLE_GEMINI_API_KEY` is listed

2. **Check .env file on VPS:**
   ```bash
   ssh username@your-vps
   cd ~/wasthere
   grep GOOGLE_GEMINI_API_KEY .env
   ```
   Should show: `GOOGLE_GEMINI_API_KEY=your-key`

3. **Check container environment:**
   ```bash
   docker exec wasthere-api env | grep GoogleGemini
   ```
   Should show: `GoogleGemini__ApiKey=your-key`

4. **Check API logs:**
   ```bash
   docker logs wasthere-api | tail -50
   ```
   Look for: "Google Gemini API key is not configured" error

### API Key Not Working Locally
1. **Verify appsettings.json has key:**
   ```bash
   cd WasThere.Api
   grep -A2 GoogleGemini appsettings.json
   ```

2. **Or set via environment variable:**
   ```bash
   export GoogleGemini__ApiKey="your-key"
   dotnet run
   ```

3. **Check application logs:**
   Look for errors related to API key configuration

## Getting a Google Gemini API Key

1. Go to: https://aistudio.google.com/app/apikey
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the key
5. Add it to GitHub Secrets or your local configuration

## Cost Considerations

Google Gemini API has usage-based pricing:
- Check current pricing at: https://ai.google.dev/pricing
- Monitor usage in Google Cloud Console
- Consider implementing rate limiting if needed
- The free tier may be sufficient for small deployments

## Environment Variable Priority

ASP.NET Core reads configuration in this order (later sources override earlier):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

Therefore, the environment variable set via Docker will override the value in `appsettings.json`.

## Related Files

- `.github/workflows/deploy.yml` - Deployment workflow that writes API key to `.env`
- `docker-compose.yml` - Passes API key to API container
- `WasThere.Api/appsettings.json` - Contains fallback key for local dev
- `WasThere.Api/Services/GoogleGeminiService.cs` - Reads the configuration
- `.env.example` - Documents the environment variable
- `.github/SECRETS-REFERENCE.md` - Lists all required secrets
