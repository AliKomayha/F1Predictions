import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { User, AuthResponse, SignupRequest, LoginRequest, VerifyPhoneRequest } from '../models/auth.model';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private apiUrl = `${environment.apiUrl}/auth`;

    // Reactive state using signals
    private currentUserSignal = signal<User | null>(null);
    private isLoadingSignal = signal<boolean>(false);

    // Public computed signals
    readonly currentUser = this.currentUserSignal.asReadonly();
    readonly isLoggedIn = computed(() => this.currentUserSignal() !== null);
    readonly isLoading = this.isLoadingSignal.asReadonly();

    constructor(
        private http: HttpClient,
        private router: Router
    ) {
        // Try to restore user from localStorage on init
        this.restoreUserFromStorage();
    }

    private restoreUserFromStorage(): void {
        const storedUser = localStorage.getItem('currentUser');
        if (storedUser) {
            try {
                this.currentUserSignal.set(JSON.parse(storedUser));
            } catch {
                localStorage.removeItem('currentUser');
            }
        }
    }

    private saveUserToStorage(user: User): void {
        localStorage.setItem('currentUser', JSON.stringify(user));
    }

    private clearUserFromStorage(): void {
        localStorage.removeItem('currentUser');
    }

    signup(request: SignupRequest): Observable<AuthResponse> {
        this.isLoadingSignal.set(true);
        return this.http.post<AuthResponse>(`${this.apiUrl}/signup`, request, { withCredentials: true })
            .pipe(
                tap(response => {
                    this.isLoadingSignal.set(false);
                    if (response.success && response.user) {
                        // User needs to verify phone - don't set as logged in yet
                        this.saveUserToStorage(response.user);
                    }
                }),
                catchError(error => {
                    this.isLoadingSignal.set(false);
                    return of({
                        success: false,
                        message: error.error?.message || 'Signup failed. Please try again.'
                    });
                })
            );
    }

    verifyPhone(request: VerifyPhoneRequest): Observable<AuthResponse> {
        this.isLoadingSignal.set(true);
        return this.http.post<AuthResponse>(`${this.apiUrl}/verify-phone`, request, { withCredentials: true })
            .pipe(
                tap(response => {
                    this.isLoadingSignal.set(false);
                    if (response.success && response.user) {
                        this.currentUserSignal.set(response.user);
                        this.saveUserToStorage(response.user);
                    }
                }),
                catchError(error => {
                    this.isLoadingSignal.set(false);
                    return of({
                        success: false,
                        message: error.error?.message || 'Verification failed. Please try again.'
                    });
                })
            );
    }

    login(request: LoginRequest): Observable<AuthResponse> {
        this.isLoadingSignal.set(true);
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request, { withCredentials: true })
            .pipe(
                tap(response => {
                    this.isLoadingSignal.set(false);
                    if (response.success && response.user) {
                        this.currentUserSignal.set(response.user);
                        this.saveUserToStorage(response.user);
                    }
                }),
                catchError(error => {
                    this.isLoadingSignal.set(false);
                    return of({
                        success: false,
                        message: error.error?.message || 'Login failed. Please try again.'
                    });
                })
            );
    }

    refreshToken(): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token`, {}, { withCredentials: true })
            .pipe(
                tap(response => {
                    if (response.success && response.user) {
                        this.currentUserSignal.set(response.user);
                        this.saveUserToStorage(response.user);
                    }
                }),
                catchError(() => {
                    // Token refresh failed - user needs to login again
                    this.clearSession();
                    return of({ success: false, message: 'Session expired' });
                })
            );
    }

    logout(): Observable<AuthResponse> {
        this.isLoadingSignal.set(true);
        return this.http.post<AuthResponse>(`${this.apiUrl}/logout`, {}, { withCredentials: true })
            .pipe(
                tap(() => {
                    this.isLoadingSignal.set(false);
                    this.clearSession();
                }),
                catchError(() => {
                    this.isLoadingSignal.set(false);
                    this.clearSession();
                    return of({ success: true, message: 'Logged out' });
                })
            );
    }

    private clearSession(): void {
        this.currentUserSignal.set(null);
        this.clearUserFromStorage();
        this.router.navigate(['/login']);
    }
}
