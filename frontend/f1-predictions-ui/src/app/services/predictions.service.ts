import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../environments/environment';
import {
    RacePrediction,
    DriverOption,
    TeamOption,
    RaceOption,
    SubmitPredictionRequest
} from '../models/prediction.model';

export interface SubmitResult {
    success: boolean;
    message: string;
}

@Injectable({
    providedIn: 'root'
})
export class PredictionsService {
    private apiUrl = `${environment.apiUrl}/predictions`;

    // Reactive state
    private predictionsSignal = signal<RacePrediction[]>([]);
    private driversSignal = signal<DriverOption[]>([]);
    private teamsSignal = signal<TeamOption[]>([]);
    private racesSignal = signal<RaceOption[]>([]);
    private isLoadingSignal = signal<boolean>(false);
    private errorSignal = signal<string>('');

    // Public readonly signals
    readonly predictions = this.predictionsSignal.asReadonly();
    readonly drivers = this.driversSignal.asReadonly();
    readonly teams = this.teamsSignal.asReadonly();
    readonly races = this.racesSignal.asReadonly();
    readonly isLoading = this.isLoadingSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();

    constructor(private http: HttpClient) { }

    getRacePredictions(raceId: number, leagueId: number): Observable<RacePrediction[]> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.get<RacePrediction[]>(
            `${this.apiUrl}/race/${raceId}/league/${leagueId}`,
            { withCredentials: true }
        ).pipe(
            tap(predictions => {
                this.predictionsSignal.set(predictions);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set(error.error?.message || error.error || 'Failed to load predictions');
                return of([]);
            })
        );
    }

    getDriversForRace(raceId: number): Observable<DriverOption[]> {
        return this.http.get<DriverOption[]>(
            `${this.apiUrl}/drivers/${raceId}`,
            { withCredentials: true }
        ).pipe(
            tap(drivers => this.driversSignal.set(drivers)),
            catchError((error: HttpErrorResponse) => {
                this.errorSignal.set('Failed to load drivers');
                return of([]);
            })
        );
    }

    getTeamsForRace(raceId: number): Observable<TeamOption[]> {
        return this.http.get<TeamOption[]>(
            `${this.apiUrl}/teams/${raceId}`,
            { withCredentials: true }
        ).pipe(
            tap(teams => this.teamsSignal.set(teams)),
            catchError((error: HttpErrorResponse) => {
                this.errorSignal.set('Failed to load teams');
                return of([]);
            })
        );
    }

    getRacesForLeague(leagueId: number): Observable<RaceOption[]> {
        this.isLoadingSignal.set(true);

        return this.http.get<RaceOption[]>(
            `${this.apiUrl}/races/${leagueId}`,
            { withCredentials: true }
        ).pipe(
            tap(races => {
                this.racesSignal.set(races);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set('Failed to load races');
                return of([]);
            })
        );
    }

    submitPrediction(request: SubmitPredictionRequest): Observable<SubmitResult> {
        return this.http.post<{ message: string }>(
            `${this.apiUrl}/submit`,
            request,
            { withCredentials: true }
        ).pipe(
            map(response => ({
                success: true,
                message: response.message
            })),
            catchError((error: HttpErrorResponse) => {
                const message = typeof error.error === 'string'
                    ? error.error
                    : (error.error?.message || 'Failed to save prediction');
                return of({
                    success: false,
                    message
                });
            })
        );
    }

    getMemberPredictions(targetUserId: number, raceId: number, leagueId: number): Observable<RacePrediction[]> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.get<RacePrediction[]>(
            `${this.apiUrl}/member/${targetUserId}/race/${raceId}/league/${leagueId}`,
            { withCredentials: true }
        ).pipe(
            tap(predictions => {
                this.predictionsSignal.set(predictions);
                this.isLoadingSignal.set(false);
            }),
            catchError((error: HttpErrorResponse) => {
                this.isLoadingSignal.set(false);
                this.errorSignal.set(error.error?.message || error.error || 'Failed to load member predictions');
                return of([]);
            })
        );
    }

    clearPredictions(): void {
        this.predictionsSignal.set([]);
    }

    clearError(): void {
        this.errorSignal.set('');
    }
}
