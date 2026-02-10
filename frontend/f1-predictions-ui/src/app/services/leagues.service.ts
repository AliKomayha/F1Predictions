import { Injectable, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { League, CreateLeagueRequest, LeagueMember } from '../models/league.model';

export interface LeagueResponse {
    success: boolean;
    data?: League;
    message: string;
}

export interface JoinLeagueResult {
    success: boolean;
    message: string;
}

@Injectable({
    providedIn: 'root'
})
export class LeaguesService {
    private apiUrl = `${environment.apiUrl}/leagues`;

    // Reactive state using signals
    private leaguesSignal = signal<League[]>([]);
    private membersSignal = signal<LeagueMember[]>([]);
    private isLoadingSignal = signal<boolean>(false);
    private errorSignal = signal<string>('');

    // Public computed signals
    readonly leagues = this.leaguesSignal.asReadonly();
    readonly members = this.membersSignal.asReadonly();
    readonly isLoading = this.isLoadingSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();
    readonly leaguesCount = computed(() => this.leaguesSignal().length);

    constructor(private http: HttpClient) { }

    getUserLeagues(): Observable<League[]> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.get<League[]>(this.apiUrl, { withCredentials: true })
            .pipe(
                tap(leagues => {
                    this.leaguesSignal.set(leagues);
                    this.isLoadingSignal.set(false);
                }),
                catchError((error: HttpErrorResponse) => {
                    this.isLoadingSignal.set(false);
                    this.errorSignal.set(error.error?.message || 'Failed to load leagues');
                    return of([]);
                })
            );
    }

    getLeagueMembers(leagueId: number): Observable<LeagueMember[]> {
        return this.http.get<LeagueMember[]>(
            `${this.apiUrl}/league-members/${leagueId}`,
            { withCredentials: true }
        ).pipe(
            tap(members => this.membersSignal.set(members)),
            catchError((error: HttpErrorResponse) => {
                this.errorSignal.set('Failed to load league members');
                return of([]);
            })
        );
    }

    clearMembers(): void {
        this.membersSignal.set([]);
    }

    createLeague(request: CreateLeagueRequest): Observable<LeagueResponse> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.post<League>(`${this.apiUrl}/create`, request, { withCredentials: true })
            .pipe(
                map(league => {
                    this.isLoadingSignal.set(false);
                    // Add the new league to the list
                    this.leaguesSignal.update(current => [league, ...current]);
                    return {
                        success: true,
                        data: league,
                        message: 'League created successfully'
                    };
                }),
                catchError((error: HttpErrorResponse) => {
                    this.isLoadingSignal.set(false);
                    const message = typeof error.error === 'string' ? error.error : (error.error?.message || 'Failed to create league');
                    this.errorSignal.set(message);
                    return of({
                        success: false,
                        message
                    });
                })
            );
    }

    joinLeagueByCode(inviteCode: string): Observable<JoinLeagueResult> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set('');

        return this.http.post<{ message: string }>(`${this.apiUrl}/join`, { inviteCode: inviteCode.trim().toUpperCase() }, { withCredentials: true })
            .pipe(
                map(response => {
                    this.isLoadingSignal.set(false);
                    // Refresh leagues after joining
                    this.getUserLeagues().subscribe();
                    return {
                        success: true,
                        message: response.message
                    };
                }),
                catchError((error: HttpErrorResponse) => {
                    this.isLoadingSignal.set(false);
                    const message = typeof error.error === 'string' ? error.error : (error.error?.message || 'Failed to join league');
                    this.errorSignal.set(message);
                    return of({
                        success: false,
                        message
                    });
                })
            );
    }

    clearError(): void {
        this.errorSignal.set('');
    }
}
