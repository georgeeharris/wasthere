import { useEffect, useState } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { useNavigate, useLocation } from 'react-router-dom';
import { usersApi } from '../services/api';

interface ProfileCheckWrapperProps {
  children: React.ReactNode;
}

/**
 * This component checks if the authenticated user has set their username.
 * If not, it redirects them to the profile page to set it up.
 */
export function ProfileCheckWrapper({ children }: ProfileCheckWrapperProps) {
  const { isAuthenticated, isLoading, getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const location = useLocation();
  const [isCheckingProfile, setIsCheckingProfile] = useState(false);
  const [hasChecked, setHasChecked] = useState(false);

  useEffect(() => {
    const checkUserProfile = async () => {
      // Don't check if:
      // - Not authenticated
      // - Still loading auth state
      // - Already on profile page
      // - Already checked
      if (!isAuthenticated || isLoading || location.pathname === '/profile' || hasChecked) {
        return;
      }

      setIsCheckingProfile(true);
      
      try {
        // Get access token first to ensure we're ready for API calls
        await getAccessTokenSilently();
        
        // Check user profile
        const profile = await usersApi.getProfile();
        
        // If username is not set, redirect to profile page
        if (!profile.username) {
          navigate('/profile', { replace: true });
        }
      } catch (error) {
        console.error('Failed to check user profile:', error);
        // Don't block the user if profile check fails
      } finally {
        setIsCheckingProfile(false);
        setHasChecked(true);
      }
    };

    checkUserProfile();
  }, [isAuthenticated, isLoading, location.pathname, navigate, getAccessTokenSilently, hasChecked]);

  // Show loading state while checking profile
  if (isAuthenticated && isCheckingProfile && location.pathname !== '/profile') {
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

  return <>{children}</>;
}
