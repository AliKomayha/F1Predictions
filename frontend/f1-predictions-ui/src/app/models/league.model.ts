export interface League {
    id: number;
    name: string;
    description?: string;
    ownerId: number;
    ownerName: string;
    championshipId: number;
    championshipName: string;
    isPublic: boolean;
    inviteCode?: string;
    createdAt: Date;
    isActive: boolean;
    memberCount: number;
}

export interface CreateLeagueRequest {
    name: string;
    description?: string;
}

export interface JoinLeagueResponse {
    message: string;
}

export interface LeagueMember {
    userId: number;
    firstName: string;
    lastName: string;
    role: string;
    joinedAt: Date;
}
