import type { Event, Venue, Act, ClubNight, ClubNightDto, Flyer, DiagnosticInfo, FlyerAnalysisResult } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// Token provider function that will be set by the app
let getAccessTokenFunction: (() => Promise<string | null>) | null = null;

export const setAccessTokenProvider = (provider: () => Promise<string | null>) => {
  getAccessTokenFunction = provider;
};

// Helper function to create authenticated fetch headers
const getAuthHeaders = async (): Promise<HeadersInit> => {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };

  if (getAccessTokenFunction) {
    try {
      const token = await getAccessTokenFunction();
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
    } catch (error) {
      console.error('Error getting access token:', error);
    }
  }

  return headers;
};

// Helper function for authenticated fetch
const authenticatedFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const authHeaders = await getAuthHeaders();
  
  const mergedHeaders = {
    ...authHeaders,
    ...options.headers,
  };

  // Remove Content-Type for FormData
  if (options.body instanceof FormData) {
    delete (mergedHeaders as Record<string, string>)['Content-Type'];
  }

  return fetch(url, {
    ...options,
    headers: mergedHeaders,
  });
};

// Events API
export const eventsApi = {
  getAll: async (): Promise<Event[]> => {
    const response = await fetch(`${API_BASE_URL}/events`);
    return response.json();
  },
  
  create: async (name: string): Promise<Event> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/events`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/events/${id}`, {
      method: 'DELETE',
    });
  },

  getDeleteImpact: async (id: number): Promise<EventDeleteImpact> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/events/${id}/delete-impact`);
    return response.json();
  },
};

// Venues API
export const venuesApi = {
  getAll: async (): Promise<Venue[]> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/venues`);
    return response.json();
  },
  
  create: async (name: string): Promise<Venue> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/venues`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/venues/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/venues/${id}`, {
      method: 'DELETE',
    });
  },

  getDeleteImpact: async (id: number): Promise<VenueDeleteImpact> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/venues/${id}/delete-impact`);
    return response.json();
  },
};

// Acts API
export const actsApi = {
  getAll: async (): Promise<Act[]> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/acts`);
    return response.json();
  },
  
  create: async (name: string): Promise<Act> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/acts`, {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/acts/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/acts/${id}`, {
      method: 'DELETE',
    });
  },

  getDeleteImpact: async (id: number): Promise<ActDeleteImpact> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/acts/${id}/delete-impact`);
    return response.json();
  },
};

// Club Nights API
export const clubNightsApi = {
  getAll: async (): Promise<ClubNight[]> => {
    const response = await fetch(`${API_BASE_URL}/clubnights`);
    return response.json();
  },
  
  create: async (dto: ClubNightDto): Promise<ClubNight> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/clubnights`, {
      method: 'POST',
      body: JSON.stringify(dto),
    });
    return response.json();
  },
  
  update: async (id: number, dto: ClubNightDto): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/clubnights/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/clubnights/${id}`, {
      method: 'DELETE',
    });
  },
};

// Flyers API
export const flyersApi = {
  getAll: async (): Promise<Flyer[]> => {
    const response = await authenticatedFetch(`${API_BASE_URL}/flyers`);
    return response.json();
  },
  
  upload: async (file: File): Promise<FlyerUploadResponse> => {
    const formData = new FormData();
    formData.append('file', file);

    // Create an AbortController with a 5-minute timeout for AI processing
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5 * 60 * 1000); // 5 minutes

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/flyers/upload`, {
        method: 'POST',
        body: formData,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to upload flyer');
      }

      return response.json();
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timed out. The AI analysis is taking longer than expected. Please try again.');
      }
      throw error;
    }
  },
  
  delete: async (id: number): Promise<void> => {
    await authenticatedFetch(`${API_BASE_URL}/flyers/${id}`, {
      method: 'DELETE',
    });
  },
  
  autoPopulate: async (id: number): Promise<AutoPopulateResult> => {
    // Create an AbortController with a 5-minute timeout for AI processing
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5 * 60 * 1000); // 5 minutes

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/flyers/${id}/auto-populate`, {
        method: 'POST',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to auto-populate from flyer');
      }

      return response.json();
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timed out. The AI analysis is taking longer than expected. Please try again.');
      }
      throw error;
    }
  },
  
  getImageUrl: (filePath: string): string => {
    // Convert relative path to API URL
    return `${API_BASE_URL.replace('/api', '')}/${filePath.replace(/\\/g, '/')}`;
  },
  
  completeUpload: async (flyerId: number, selectedYears: YearSelection[], eventId?: number): Promise<AutoPopulateResult> => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5 * 60 * 1000); // 5 minutes

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/flyers/${flyerId}/complete-upload`, {
        method: 'POST',
        body: JSON.stringify({ selectedYears, eventId }),
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to complete upload');
      }

      return response.json();
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timed out. The operation is taking longer than expected. Please try again.');
      }
      throw error;
    }
  },
};

export interface FlyerUploadResponse {
  success: boolean;
  message: string;
  flyer?: Flyer;
  autoPopulateResult?: AutoPopulateResult;
  diagnostics?: DiagnosticInfo;
  analysisResult?: FlyerAnalysisResult;
  needsEventSelection?: boolean;
}

export interface YearSelection {
  month: number;
  day: number;
  year: number;
}

export interface AutoPopulateResult {
  success: boolean;
  message: string;
  clubNightsCreated: number;
  eventsCreated: number;
  venuesCreated: number;
  actsCreated: number;
  errors: string[];
  diagnostics?: DiagnosticInfo;
}

export interface EventDeleteImpact {
  clubNightsCount: number;
  flyersCount: number;
}

export interface VenueDeleteImpact {
  clubNightsCount: number;
  flyersCount: number;
}

export interface ActDeleteImpact {
  clubNightActsCount: number;
}
