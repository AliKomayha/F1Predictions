import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LeaguesService } from '../../services/leagues.service';
import { AuthService } from '../../services/auth.service';
import { League } from '../../models/league.model';

@Component({
    selector: 'app-predictions',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './predictions.component.html',
    styleUrl: './predictions.component.scss'
})
export class PredictionsComponent implements OnInit {

    constructor(
        public leaguesService: LeaguesService,
        public authService: AuthService,
        private router: Router
    ) { }

    ngOnInit(): void {
        if (this.authService.isLoggedIn()) {
            this.leaguesService.getUserLeagues().subscribe();
        }
    }

    openLeague(league: League): void {
        this.router.navigate(['/predictions', 'league', league.id]);
    }

    isOwner(league: League): boolean {
        const user = this.authService.currentUser();
        return user !== null && league.ownerId === user.id;
    }
}
