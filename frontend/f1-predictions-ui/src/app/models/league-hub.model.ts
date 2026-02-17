// League Hub models for FPL-inspired redesign

export interface CurrentRace {
    id: number;
    raceName: string;
    roundNumber: number;
    totalRounds: number;
    raceDate: string;
    trackName: string;
    predictionsLockedAt: string;
    arePredictionsLocked: boolean;
    raceState: string;
    isVotingOpen: boolean;
    votingClosesAt?: string;
}

export interface LeagueSummary {
    leagueId: number;
    leagueName: string;
    currentRace: CurrentRace;
    userTotalPoints: number;
    userRacePoints: number;
    members: MemberStanding[];
}

export interface MemberStanding {
    userId: number;
    firstName: string;
    lastName: string;
    role: string;
    totalPoints: number;
    rank: number;
    hasUndoneVotes: boolean;
}

export interface MemberPrediction {
    weeklyPredictionId: number;
    predictionType: string;
    adminDefinedText?: string;
    allowedTargetTypes: string;
    userPick?: {
        id: number;
        targetType: string;
        driverId?: number;
        driverName?: string;
        teamId?: number;
        teamName?: string;
        text?: string;
        isLocked: boolean;
    };
    pointsAwarded?: number;
    pointsStatus: string; // Correct, Wrong, VotingInProgress, Pending
    isVotable: boolean;
    yesVotes: number;
    noVotes: number;
    myVote?: boolean;
    isVoteResolved: boolean;
}

export interface VoteResult {
    voteRecorded: boolean;
    yesVotes: number;
    noVotes: number;
    wasAutoResolved: boolean;
    resolution?: boolean;
}

export interface VotingStatus {
    isVotingOpen: boolean;
    opensAt?: string;
    closesAt?: string;
    raceState: string;
}
