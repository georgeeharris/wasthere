export interface Event {
  id: number;
  name: string;
}

export interface Venue {
  id: number;
  name: string;
}

export interface Act {
  id: number;
  name: string;
}

export interface ClubNight {
  id: number;
  date: string;
  eventId: number;
  eventName: string;
  venueId: number;
  venueName: string;
  acts: { actId: number; actName: string }[];
}

export interface ClubNightDto {
  date: string;
  eventId: number;
  venueId: number;
  actIds: number[];
}
