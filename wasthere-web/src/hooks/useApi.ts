import { useAuth0 } from '@auth0/auth0-react';

export const useApi = () => {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0();

  const getAuthHeaders = async (): Promise<HeadersInit> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (isAuthenticated) {
      try {
        const token = await getAccessTokenSilently();
        headers['Authorization'] = `Bearer ${token}`;
      } catch (error) {
        console.error('Error getting access token:', error);
      }
    }

    return headers;
  };

  const authenticatedFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
    const headers = await getAuthHeaders();
    
    // Merge headers, but preserve Content-Type if it's already set (e.g., for FormData)
    const mergedOptions = {
      ...options,
      headers: {
        ...headers,
        ...options.headers,
      },
    };

    // Remove Content-Type for FormData
    if (options.body instanceof FormData) {
      delete (mergedOptions.headers as Record<string, string>)['Content-Type'];
    }

    return fetch(url, mergedOptions);
  };

  return { authenticatedFetch, getAuthHeaders };
};
