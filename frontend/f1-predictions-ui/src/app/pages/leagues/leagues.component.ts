import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LeaguesService } from '../../services/leagues.service';
import { AuthService } from '../../services/auth.service';
import { League } from '../../models/league.model';

@Component({
    selector: 'app-leagues',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    templateUrl: './leagues.component.html',
    styleUrl: './leagues.component.scss'
})
export class LeaguesComponent implements OnInit {
    // Create League Modal
    showCreateModal = signal<boolean>(false);
    newLeagueName = '';
    newLeagueDescription = '';
    createError = signal<string>('');
    createSuccess = signal<string>('');

    // Join League Modal
    showJoinModal = signal<boolean>(false);
    inviteCode = '';
    joinError = signal<string>('');
    joinSuccess = signal<string>('');

    // Copy feedback
    copiedLeagueId = signal<number | null>(null);

    constructor(
        public leaguesService: LeaguesService,
        public authService: AuthService
    ) { }

    ngOnInit(): void {
        if (this.authService.isLoggedIn()) {
            this.leaguesService.getUserLeagues().subscribe();
        }
    }

    openCreateModal(): void {
        this.showCreateModal.set(true);
        this.createError.set('');
        this.createSuccess.set('');
        this.newLeagueName = '';
        this.newLeagueDescription = '';
    }

    closeCreateModal(): void {
        this.showCreateModal.set(false);
    }

    openJoinModal(): void {
        this.showJoinModal.set(true);
        this.joinError.set('');
        this.joinSuccess.set('');
        this.inviteCode = '';
    }

    closeJoinModal(): void {
        this.showJoinModal.set(false);
    }

    onCreateLeague(): void {
        if (!this.newLeagueName.trim()) {
            this.createError.set('Please enter a league name');
            return;
        }

        this.createError.set('');
        this.leaguesService.createLeague({
            name: this.newLeagueName.trim(),
            description: this.newLeagueDescription.trim() || undefined
        }).subscribe(response => {
            if (response.success) {
                this.createSuccess.set('League created successfully!');
                setTimeout(() => {
                    this.closeCreateModal();
                    this.leaguesService.getUserLeagues().subscribe();
                }, 1500);
            } else {
                this.createError.set(response.message);
            }
        });
    }

    onJoinLeague(): void {
        if (!this.inviteCode.trim()) {
            this.joinError.set('Please enter an invite code');
            return;
        }

        this.joinError.set('');
        this.leaguesService.joinLeagueByCode(this.inviteCode.trim()).subscribe(response => {
            if (response.success) {
                this.joinSuccess.set(response.message);
                setTimeout(() => {
                    this.closeJoinModal();
                }, 1500);
            } else {
                this.joinError.set(response.message);
            }
        });
    }

    copyInviteCode(league: League): void {
        if (league.inviteCode) {
            navigator.clipboard.writeText(league.inviteCode).then(() => {
                this.copiedLeagueId.set(league.id);
                setTimeout(() => {
                    this.copiedLeagueId.set(null);
                }, 2000);
            });
        }
    }

    isOwner(league: League): boolean {
        const currentUser = this.authService.currentUser();
        return currentUser ? league.ownerId === currentUser.id : false;
    }
}
