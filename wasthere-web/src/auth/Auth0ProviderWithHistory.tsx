import { Auth0Provider } from '@auth0/auth0-react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';

interface Auth0ProviderWithHistoryProps {
  children: ReactNode;
}

const Auth0ProviderWithHistory = ({ children }: Auth0ProviderWithHistoryProps) => {
  const navigate = useNavigate();
  const domain = import.meta.env.VITE_AUTH0_DOMAIN;
  const clientId = import.meta.env.VITE_AUTH0_CLIENT_ID;
  const audience = import.meta.env.VITE_AUTH0_AUDIENCE;

  console.log('Auth0 Config:', {
    domain,
    clientId: clientId ? `${clientId.substring(0, 10)}...` : 'missing',
    audience,
    hasAll: !!(domain && clientId)
  });

  if (!domain || !clientId) {
    console.warn(
      'Auth0 not configured properly. Please set the following environment variables:\n' +
      `  VITE_AUTH0_DOMAIN: ${domain ? '✓' : '✗ Missing'}\n` +
      `  VITE_AUTH0_CLIENT_ID: ${clientId ? '✓' : '✗ Missing'}\n` +
      `  VITE_AUTH0_AUDIENCE: ${audience ? '✓' : '✗ Missing (optional)'}\n` +
      'See AUTH0-SETUP.md for configuration instructions.'
    );
    return <>{children}</>;
  }

  const onRedirectCallback = (appState?: { returnTo?: string }) => {
    console.log('Auth0 redirect callback triggered', { appState });
    // Navigate to the returnTo route after successful login, or default to timeline
    navigate(appState?.returnTo || '/timeline');
  };

  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience: audience,
      }}
      onRedirectCallback={onRedirectCallback}
      useRefreshTokens={true}
      cacheLocation="localstorage"
    >
      {children}
    </Auth0Provider>
  );
};

export default Auth0ProviderWithHistory;
