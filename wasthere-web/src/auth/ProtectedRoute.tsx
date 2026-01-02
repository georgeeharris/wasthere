import { useAuth0 } from '@auth0/auth0-react';
import type { ReactNode } from 'react';
import { useLocation } from 'react-router-dom';

interface ProtectedRouteProps {
  children: ReactNode;
}

const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
  const location = useLocation();

  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        Loading...
      </div>
    );
  }

  if (!isAuthenticated) {
    // Trigger login and preserve the current location for post-login redirect
    loginWithRedirect({
      appState: { returnTo: location.pathname }
    }).catch((error) => {
      console.error('Login redirect failed:', error);
    });
    return null;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
