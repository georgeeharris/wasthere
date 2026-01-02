import { Auth0Provider } from '@auth0/auth0-react';
import type { ReactNode } from 'react';

interface Auth0ProviderWithHistoryProps {
  children: ReactNode;
}

const Auth0ProviderWithHistory = ({ children }: Auth0ProviderWithHistoryProps) => {
  const domain = import.meta.env.VITE_AUTH0_DOMAIN;
  const clientId = import.meta.env.VITE_AUTH0_CLIENT_ID;
  const audience = import.meta.env.VITE_AUTH0_AUDIENCE;

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

  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience: audience,
      }}
    >
      {children}
    </Auth0Provider>
  );
};

export default Auth0ProviderWithHistory;
