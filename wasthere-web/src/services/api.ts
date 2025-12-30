import type { Event, Venue, Act, ClubNight, ClubNightDto, Flyer } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// Events API
export const eventsApi = {
  getAll: async (): Promise<Event[]> => {
    const response = await fetch(`${API_BASE_URL}/events`);
    return response.json();
  },
  
  create: async (name: string): Promise<Event> => {
    const response = await fetch(`${API_BASE_URL}/events`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await fetch(`${API_BASE_URL}/events/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await fetch(`${API_BASE_URL}/events/${id}`, {
      method: 'DELETE',
    });
  },
};

// Venues API
export const venuesApi = {
  getAll: async (): Promise<Venue[]> => {
    const response = await fetch(`${API_BASE_URL}/venues`);
    return response.json();
  },
  
  create: async (name: string): Promise<Venue> => {
    const response = await fetch(`${API_BASE_URL}/venues`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await fetch(`${API_BASE_URL}/venues/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await fetch(`${API_BASE_URL}/venues/${id}`, {
      method: 'DELETE',
    });
  },
};

// Acts API
export const actsApi = {
  getAll: async (): Promise<Act[]> => {
    const response = await fetch(`${API_BASE_URL}/acts`);
    return response.json();
  },
  
  create: async (name: string): Promise<Act> => {
    const response = await fetch(`${API_BASE_URL}/acts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    return response.json();
  },
  
  update: async (id: number, name: string): Promise<void> => {
    await fetch(`${API_BASE_URL}/acts/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, name }),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await fetch(`${API_BASE_URL}/acts/${id}`, {
      method: 'DELETE',
    });
  },
};

// Club Nights API
export const clubNightsApi = {
  getAll: async (): Promise<ClubNight[]> => {
    const response = await fetch(`${API_BASE_URL}/clubnights`);
    return response.json();
  },
  
  create: async (dto: ClubNightDto): Promise<ClubNight> => {
    const response = await fetch(`${API_BASE_URL}/clubnights`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(dto),
    });
    return response.json();
  },
  
  update: async (id: number, dto: ClubNightDto): Promise<void> => {
    await fetch(`${API_BASE_URL}/clubnights/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(dto),
    });
  },
  
  delete: async (id: number): Promise<void> => {
    await fetch(`${API_BASE_URL}/clubnights/${id}`, {
      method: 'DELETE',
    });
  },
};

// Flyers API
export const flyersApi = {
  getAll: async (): Promise<Flyer[]> => {
    const response = await fetch(`${API_BASE_URL}/flyers`);
    return response.json();
  },
  
  upload: async (
    file: File,
    eventId: number,
    venueId: number,
    earliestClubNightDate: string
  ): Promise<Flyer> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('eventId', eventId.toString());
    formData.append('venueId', venueId.toString());
    formData.append('earliestClubNightDate', earliestClubNightDate);

    const response = await fetch(`${API_BASE_URL}/flyers/upload`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to upload flyer');
    }

    return response.json();
  },
  
  delete: async (id: number): Promise<void> => {
    await fetch(`${API_BASE_URL}/flyers/${id}`, {
      method: 'DELETE',
    });
  },
  
  autoPopulate: async (id: number): Promise<AutoPopulateResult> => {
    const response = await fetch(`${API_BASE_URL}/flyers/${id}/auto-populate`, {
      method: 'POST',
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to auto-populate from flyer');
    }

    return response.json();
  },
  
  getImageUrl: (filePath: string): string => {
    // Convert relative path to API URL
    return `${API_BASE_URL.replace('/api', '')}/${filePath.replace(/\\/g, '/')}`;
  },
};

export interface AutoPopulateResult {
  success: boolean;
  message: string;
  clubNightsCreated: number;
  eventsCreated: number;
  venuesCreated: number;
  actsCreated: number;
  errors: string[];
}
