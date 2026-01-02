# Auth0 Setup Guide

This guide explains how to configure Auth0 authentication for the WasThere application.

## Prerequisites

- An Auth0 account (free tier is sufficient)
- Access to your Auth0 dashboard

## Auth0 Configuration Steps

### 1. Create an Auth0 Application (Frontend)

1. Log in to your [Auth0 Dashboard](https://manage.auth0.com/)
2. Navigate to **Applications** > **Applications**
3. Click **Create Application**
4. Name it "WasThere Web App" (or similar)
5. Select **Single Page Application**
6. Click **Create**

In the application settings:
- **Allowed Callback URLs**: Add your application URL(s):
  - For local development: `http://localhost:5173, http://localhost:3000, http://localhost`
  - For production: `http://www.wasthere.co.uk, https://www.wasthere.co.uk`
- **Allowed Logout URLs**: Same as Callback URLs
- **Allowed Web Origins**: Same as Callback URLs
- **Allowed Origins (CORS)**: Same as Callback URLs

Save the following values:
- **Domain**: e.g., `your-tenant.auth0.com`
- **Client ID**: e.g., `abc123xyz456...`

### 2. Create an Auth0 API (Backend)

1. In Auth0 Dashboard, navigate to **Applications** > **APIs**
2. Click **Create API**
3. Name it "WasThere API" (or similar)
4. Set **Identifier** to: `https://wasthere-api` (or your preferred identifier)
5. Leave **Signing Algorithm** as RS256
6. Click **Create**

Save the following value:
- **Identifier**: This is your **Audience** value (e.g., `https://wasthere-api`)

### 3. Configure Environment Variables

#### For Local Development

Create a `.env` file in the root directory based on `.env.example`:

```bash
# Auth0 Configuration
AUTH0_DOMAIN=your-tenant.auth0.com
AUTH0_AUDIENCE=https://wasthere-api
AUTH0_CLIENT_ID=your_client_id_here
```

For the frontend, create `wasthere-web/.env` based on `wasthere-web/.env.example`:

```bash
# Auth0 Configuration
VITE_AUTH0_DOMAIN=your-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=your_client_id_here
VITE_AUTH0_AUDIENCE=https://wasthere-api

# API Configuration
VITE_API_URL=http://localhost:5000/api
```

#### For Docker Deployment

Update your `.env` file with the Auth0 configuration:

```bash
AUTH0_DOMAIN=your-tenant.auth0.com
AUTH0_AUDIENCE=https://wasthere-api
AUTH0_CLIENT_ID=your_client_id_here
```

The Docker Compose file will automatically pass these to the containers.

#### For GitHub Actions / CI/CD

Add the following secrets to your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
4. Add the following secrets:
   - `AUTH0_DOMAIN`: Your Auth0 domain
   - `AUTH0_AUDIENCE`: Your Auth0 API identifier
   - `AUTH0_CLIENT_ID`: Your Auth0 client ID

## Testing the Authentication

### Timeline Page (Public)
- Navigate to `/timeline`
- Should be accessible without logging in
- Can view events and club nights
- Cannot create/edit/delete data

### Protected Pages (Require Login)
- Navigate to `/nights`, `/flyers`, or `/master`
- Should automatically redirect to Auth0 login
- After successful login, should be redirected back to the requested page
- Can create/edit/delete data

### Logout
- Click the "Log Out" button in the header
- Should redirect to Auth0 logout
- Should return to the timeline page
- Should no longer have access to protected pages

## Troubleshooting

### "Callback URL mismatch" error
- Ensure your application URL is added to **Allowed Callback URLs** in Auth0
- Check that the URL exactly matches (including protocol and port)

### "Invalid audience" error
- Verify the `AUTH0_AUDIENCE` matches the **Identifier** in your Auth0 API configuration
- Ensure the audience is being passed correctly in environment variables

### CORS errors
- Verify your application URL is added to **Allowed Web Origins** in Auth0
- Check that `CORS_ORIGINS` in your `.env` includes your frontend URL

### Token not being sent to API
- Check browser console for errors
- Verify that protected routes are using the `authenticatedFetch` function
- Ensure Auth0Provider is properly wrapping the App component

## Security Notes

- **Never commit** your Auth0 credentials to version control
- Use environment variables or GitHub Secrets for all Auth0 configuration
- The free tier of Auth0 is suitable for development and small projects
- For production, consider Auth0's security best practices and rate limits
- The client ID is not sensitive, but the domain and audience should be kept private
