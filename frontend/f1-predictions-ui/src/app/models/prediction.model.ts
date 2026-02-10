export interface RacePrediction {
    weeklyPredictionId: number;
    predictionType: string;
    adminDefinedText?: string;
    allowedTargetTypes: string;
    userPick?: UserPrediction;
}

export interface UserPrediction {
    id: number;
    targetType: string;
    driverId?: number;
    driverName?: string;
    teamId?: number;
    teamName?: string;
    text?: string;
    isLocked: boolean;
}

export interface SubmitPredictionRequest {
    weeklyPredictionId: number;
    leagueId: number;
    targetType: string;
    driverId?: number;
    teamId?: number;
    text?: string;
}

export interface DriverOption {
    id: number;
    firstName: string;
    lastName: string;
    championshipNumber: number;
    teamName: string;
}

export interface TeamOption {
    id: number;
    name: string;
    displayName: string;
}

export interface RaceOption {
    id: number;
    raceName: string;
    roundNumber: number;
    raceDate: Date;
    trackName: string;
    predictionsLockedAt: Date;
}
