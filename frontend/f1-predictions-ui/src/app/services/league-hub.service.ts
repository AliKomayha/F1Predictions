import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../environments/environment';
import {
    CurrentRace,
    LeagueSummary,
    MemberPrediction,
    VoteResult,
    VotingStatus
} from '../models/league-hub.model';

@Injectable({
    providedIn: 'root'
})
export class LeagueHubService {
    private apiUrl = `${environment.apiUrl}/predictions`;
    private votingUrl = `${environment.apiUrl}/voting`;

    // Reactive state
    private summarySignal = signal<LeagueSummary | null>(null);
    private currentRaceSignal = signal<CurrentRace | null>(null);
    private memberPredictionsSignal = signal<MemberPrediction[]>([]);
    private isLoadingSignal = signal<boolean>(false);
    private errorSignal = signal<string>('');

    readonly summary = this.summarySignal.asReadonly();
    readonly currentRace = this.currentRaceSignal.asReadonly();
    readonly memberPredictions = this.memberPredictionsSignal.asReadonly();
    readonly isLoading = this.isLoadingSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();

    constructor(private http: HttpClient) { }

    getCurrentRace(leagueId: number, raceId?: number): Observable<CurrentRace> {
        this.isLoadingSignal.set(true);
        const url = raceId
            ? `${this.apiUrl}/current-race/${leagueId}?raceId=${raceId}`
            : `${this.apiUrl}/current-race/${leagueId}`;

        return this.http.get<CurrentRace>(url, { withCredentials: true }).pipe(
            tap(race => {
                this.currentRaceSignal.set(race);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set(error.error?.message || 'Failed to load race');
                return of(null as any);
            })
        );
    }

    getLeagueSummary(leagueId: number, raceId: number): Observable<LeagueSummary> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.get<LeagueSummary>(
            `${this.apiUrl}/league-summary/${leagueId}/${raceId}`,
            { withCredentials: true }
        ).pipe(
            tap(summary => {
                this.summarySignal.set(summary);
                this.currentRaceSignal.set(summary.currentRace);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set(error.error?.message || 'Failed to load league summary');
                return of(null as any);
            })
        );
    }

    getMemberPredictions(targetUserId: number, raceId: number, leagueId: number): Observable<MemberPrediction[]> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.get<MemberPrediction[]>(
            `${this.apiUrl}/member-predictions/${targetUserId}/${raceId}/${leagueId}`,
            { withCredentials: true }
        ).pipe(
            tap(predictions => {
                this.memberPredictionsSignal.set(predictions);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set(error.error?.message || 'Failed to load predictions');
                return of([]);
            })
        );
    }

    castVote(userPredictionId: number, vote: boolean): Observable<VoteResult> {
        return this.http.post<VoteResult>(
            `${this.votingUrl}/cast`,
            { userPredictionId, vote },
            { withCredentials: true }
        ).pipe(
            catchError((error: HttpErrorResponse) => {
                this.errorSignal.set(error.error?.message || 'Failed to cast vote');
                return of(null as any);
            })
        );
    }

    reportPrediction(userPredictionId: number, reason: string): Observable<any> {
        return this.http.post(
            `${this.votingUrl}/report`,
            { userPredictionId, reason },
            { withCredentials: true }
        ).pipe(
            catchError((error: HttpErrorResponse) => {
                this.errorSignal.set(error.error?.message || 'Failed to submit report');
                return of(null);
            })
        );
    }

    clearState(): void {
        this.summarySignal.set(null);
        this.currentRaceSignal.set(null);
        this.memberPredictionsSignal.set([]);
        this.errorSignal.set('');
    }
}
