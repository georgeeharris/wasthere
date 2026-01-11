import { useState, useEffect } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { useNavigate } from 'react-router-dom';
import { usersApi } from '../services/api';
import type { User } from '../types';
import '../styles/Profile.css';

export function Profile() {
  const { isAuthenticated, isLoading, user: auth0User } = useAuth0();
  const navigate = useNavigate();
  const [user, setUser] = useState<User | null>(null);
  const [username, setUsername] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const [isCheckingUsername, setIsCheckingUsername] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate('/');
      return;
    }

    if (isAuthenticated) {
      loadProfile();
    }
  }, [isAuthenticated, isLoading, navigate]);

  const loadProfile = async () => {
    try {
      const profile = await usersApi.getProfile();
      setUser(profile);
      setUsername(profile.username || '');
    } catch (err) {
      console.error('Failed to load profile:', err);
      setError('Failed to load profile');
    }
  };

  const validateUsername = async (value: string) => {
    // Clear previous validation
    setValidationMessage(null);
    
    if (!value) {
      setValidationMessage('Username is required');
      return false;
    }

    if (value.length < 3 || value.length > 20) {
      setValidationMessage('Username must be between 3 and 20 characters');
      return false;
    }

    if (!/^[a-zA-Z0-9_-]+$/.test(value)) {
      setValidationMessage('Username can only contain letters, numbers, hyphens, and underscores');
      return false;
    }

    // Check availability if different from current username
    if (value !== user?.username) {
      setIsCheckingUsername(true);
      try {
        const result = await usersApi.checkUsername(value);
        setIsCheckingUsername(false);
        if (!result.available) {
          setValidationMessage(result.message || 'Username is not available');
          return false;
        }
      } catch (err) {
        setIsCheckingUsername(false);
        console.error('Failed to check username:', err);
        return false;
      }
    }

    return true;
  };

  const handleUsernameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setUsername(value);
    setError(null);
    setSuccess(null);
    
    // Debounce validation
    if (value) {
      const timeoutId = setTimeout(() => {
        validateUsername(value);
      }, 500);
      return () => clearTimeout(timeoutId);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSubmitting(true);

    const isValid = await validateUsername(username);
    if (!isValid) {
      setIsSubmitting(false);
      return;
    }

    try {
      const updatedUser = await usersApi.updateProfile(username);
      setUser(updatedUser);
      setSuccess('Profile updated successfully!');
      
      // If this was a forced profile setup (no previous username), redirect to timeline after 2 seconds
      if (!user?.username) {
        setTimeout(() => {
          navigate('/timeline');
        }, 2000);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update profile';
      setError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="profile-container">
        <div className="profile-loading">Loading...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  const isNewUser = !user?.username;

  return (
    <div className="profile-container">
      <div className="profile-card">
        <h2>User Profile</h2>
        
        {isNewUser && (
          <div className="profile-welcome">
            <p className="welcome-message">
              Welcome! Please choose a username to continue.
            </p>
            <p className="welcome-subtext">
              This username will be your visible identifier to other users.
            </p>
          </div>
        )}

        <form onSubmit={handleSubmit} className="profile-form">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={handleUsernameChange}
              placeholder="Enter your username"
              disabled={isSubmitting}
              className={validationMessage ? 'input-error' : ''}
              autoFocus
            />
            {isCheckingUsername && (
              <p className="validation-message checking">Checking availability...</p>
            )}
            {validationMessage && !isCheckingUsername && (
              <p className="validation-message error">{validationMessage}</p>
            )}
            <p className="input-hint">
              3-20 characters, letters, numbers, hyphens, and underscores only
            </p>
          </div>

          {error && (
            <div className="alert alert-error">{error}</div>
          )}

          {success && (
            <div className="alert alert-success">{success}</div>
          )}

          <div className="form-actions">
            <button 
              type="submit" 
              className="btn btn-primary"
              disabled={isSubmitting || isCheckingUsername || !username || !!validationMessage}
            >
              {isSubmitting ? 'Saving...' : isNewUser ? 'Continue' : 'Update Username'}
            </button>
            
            {!isNewUser && (
              <button 
                type="button" 
                className="btn btn-secondary"
                onClick={() => navigate('/timeline')}
                disabled={isSubmitting}
              >
                Cancel
              </button>
            )}
          </div>
        </form>

        {auth0User && (
          <div className="profile-info">
            <p className="info-label">Logged in as:</p>
            <p className="info-value">{auth0User.email || auth0User.name || 'User'}</p>
          </div>
        )}
      </div>
    </div>
  );
}
