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

export interface ClubNightAct {
  actId: number;
  actName: string;
  isLiveSet: boolean;
}

export interface ClubNight {
  id: number;
  date: string;
  eventId: number;
  eventName: string;
  venueId: number;
  venueName: string;
  acts: ClubNightAct[];
}

export interface ClubNightActDto {
  actId: number;
  isLiveSet: boolean;
}

export interface ClubNightDto {
  date: string;
  eventId: number;
  venueId: number;
  acts: ClubNightActDto[];
}

export interface Flyer {
  id: number;
  filePath: string;
  fileName: string;
  uploadedAt: string;
  eventId: number;
  event?: Event;
  venueId: number;
  venue?: Venue;
  earliestClubNightDate: string;
}
