import { Component } from '@angular/core';

@Component({
    selector: 'app-home',
    standalone: true,
    imports: [],
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss'
})
export class HomeComponent {
    // Featured race card data (placeholder)
    nextRace = {
        name: 'Australian Grand Prix',
        track: 'Albert Park Circuit',
        date: 'March 16, 2025',
        countryFlag: '🇦🇺',
        round: 1
    };

    // Quick stats for the user
    userStats = {
        totalPoints: 245,
        rank: 12,
        correctPredictions: 18,
        totalPredictions: 32
    };

    // Recent activity items
    recentActivity = [
        { type: 'prediction', message: 'You predicted Verstappen for pole position', time: '2 hours ago' },
        { type: 'points', message: 'Earned 10 points from Bahrain GP', time: '1 day ago' },
        { type: 'league', message: 'Moved up 3 positions in your league', time: '2 days ago' }
    ];
}
