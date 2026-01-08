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
  flyerId?: number | null;
  flyerFilePath?: string | null;
  flyerThumbnailPath?: string | null;
  acts: ClubNightAct[];
  wasThereByAdmin?: boolean;
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
  thumbnailPath?: string | null;
  fileName: string;
  uploadedAt: string;
  eventId: number;
  event?: Event;
  venueId: number;
  venue?: Venue;
  earliestClubNightDate: string;
}

export interface DiagnosticStep {
  name: string;
  status: string;
  timestamp: string;
  durationMs?: number | null;
  details?: string | null;
  error?: string | null;
}

export interface DiagnosticInfo {
  logId?: string | null;
  steps: DiagnosticStep[];
  metadata: Record<string, string>;
  errorMessage?: string | null;
  stackTrace?: string | null;
}

export interface ClubNightData {
  eventName?: string | null;
  venueName?: string | null;
  date?: string | null;
  dayOfWeek?: string | null;
  month?: number | null;
  day?: number | null;
  candidateYears: number[];
  acts: ActData[];
}

export interface ActData {
  name: string;
  isLiveSet: boolean;
}

export interface FlyerData {
  clubNights: ClubNightData[];
}

export interface FlyerAnalysisResult {
  success: boolean;
  errorMessage?: string | null;
  flyers: FlyerData[];
  diagnostics: DiagnosticInfo;
  // Legacy property for backward compatibility
  clubNights: ClubNightData[];
}
