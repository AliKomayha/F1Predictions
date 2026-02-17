import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LeagueHubService } from '../../services/league-hub.service';
import { PredictionsService } from '../../services/predictions.service';
import { AuthService } from '../../services/auth.service';
import { LeaguesService } from '../../services/leagues.service';
import { MemberStanding, MemberPrediction } from '../../models/league-hub.model';
import { RacePrediction, DriverOption, TeamOption, SubmitPredictionRequest } from '../../models/prediction.model';

type ViewMode = 'hub' | 'myPredictions' | 'makePrediction' | 'memberView';

interface PredictionForm {
    weeklyPredictionId: number;
    targetType: string;
    driverId?: number;
    teamId?: number;
    text?: string;
    saving: boolean;
    success: string;
    error: string;
}

@Component({
    selector: 'app-league-hub',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './league-hub.component.html',
    styleUrl: './league-hub.component.scss'
})
export class LeagueHubComponent implements OnInit {
    leagueId = 0;
    viewMode = signal<ViewMode>('hub');
    selectedMember = signal<MemberStanding | null>(null);

    // Prediction form state
    predictionForms = signal<Map<number, PredictionForm>>(new Map());
    drivers = signal<DriverOption[]>([]);
    teams = signal<TeamOption[]>([]);

    // Report modal
    showReportModal = signal<boolean>(false);
    reportPredictionId = signal<number>(0);
    reportReason = signal<string>('');
    reportSubmitting = signal<boolean>(false);

    // Voting feedback
    votingFeedback = signal<Map<number, string>>(new Map());

    constructor(
        public hubService: LeagueHubService,
        public predictionsService: PredictionsService,
        public authService: AuthService,
        public leaguesService: LeaguesService,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.leagueId = Number(this.route.snapshot.paramMap.get('leagueId'));
        this.loadCurrentRace();
    }

    // === RACE NAVIGATION ===

    loadCurrentRace(raceId?: number): void {
        this.hubService.getCurrentRace(this.leagueId, raceId).subscribe(race => {
            if (race) {
                this.loadLeagueSummary(race.id);
            }
        });
    }

    goToPreviousRace(): void {
        const race = this.hubService.currentRace();
        if (race && race.roundNumber > 1) {
            // Need to find the previous race ID
            this.predictionsService.getRacesForLeague(this.leagueId).subscribe(races => {
                const prevRace = races.find(r => r.roundNumber === race.roundNumber - 1);
                if (prevRace) {
                    this.viewMode.set('hub');
                    this.loadCurrentRace(prevRace.id);
                }
            });
        }
    }

    goToNextRace(): void {
        const race = this.hubService.currentRace();
        if (race && race.roundNumber < race.totalRounds) {
            this.predictionsService.getRacesForLeague(this.leagueId).subscribe(races => {
                const nextRace = races.find(r => r.roundNumber === race.roundNumber + 1);
                if (nextRace) {
                    this.viewMode.set('hub');
                    this.loadCurrentRace(nextRace.id);
                }
            });
        }
    }

    loadLeagueSummary(raceId: number): void {
        this.hubService.getLeagueSummary(this.leagueId, raceId).subscribe();
    }

    // === VIEW MODES ===

    showMyPredictions(): void {
        const race = this.hubService.currentRace();
        const user = this.authService.currentUser();
        if (race && user) {
            this.hubService.getMemberPredictions(user.id, race.id, this.leagueId).subscribe();
            this.viewMode.set('myPredictions');
        }
    }

    showMakePrediction(): void {
        const race = this.hubService.currentRace();
        if (!race) return;

        // Load predictions, drivers, and teams
        forkJoin({
            predictions: this.predictionsService.getRacePredictions(race.id, this.leagueId),
            drivers: this.predictionsService.getDriversForRace(race.id),
            teams: this.predictionsService.getTeamsForRace(race.id)
        }).subscribe(({ predictions, drivers, teams }) => {
            this.drivers.set(drivers);
            this.teams.set(teams);
            this.initForms(predictions);
            this.viewMode.set('makePrediction');
        });
    }

    showMemberPredictions(member: MemberStanding): void {
        const race = this.hubService.currentRace();
        if (!race) return;

        this.selectedMember.set(member);
        this.hubService.getMemberPredictions(member.userId, race.id, this.leagueId).subscribe();
        this.viewMode.set('memberView');
    }

    backToHub(): void {
        this.viewMode.set('hub');
        this.selectedMember.set(null);
        this.votingFeedback.set(new Map());
    }

    goBack(): void {
        this.router.navigate(['/predictions']);
    }

    // === PREDICTION FORM ===

    initForms(predictions: RacePrediction[]): void {
        const forms = new Map<number, PredictionForm>();
        for (const pred of predictions) {
            forms.set(pred.weeklyPredictionId, {
                weeklyPredictionId: pred.weeklyPredictionId,
                targetType: pred.userPick?.targetType || '',
                driverId: pred.userPick?.driverId,
                teamId: pred.userPick?.teamId,
                text: pred.userPick?.text,
                saving: false,
                success: '',
                error: ''
            });
        }
        this.predictionForms.set(forms);
    }

    getForm(id: number): PredictionForm | undefined {
        return this.predictionForms().get(id);
    }

    getAllowedTypes(prediction: RacePrediction): string[] {
        return prediction.allowedTargetTypes.split(',').map(t => t.trim());
    }

    onTargetTypeChange(id: number, type: string): void {
        this.updateForm(id, { targetType: type, driverId: undefined, teamId: undefined, text: undefined });
    }

    onDriverChange(id: number, event: Event): void {
        const val = +(event.target as HTMLSelectElement).value;
        this.updateForm(id, { driverId: val || undefined });
    }

    onTeamChange(id: number, event: Event): void {
        const val = +(event.target as HTMLSelectElement).value;
        this.updateForm(id, { teamId: val || undefined });
    }

    onTextChange(id: number, event: Event): void {
        const val = (event.target as HTMLInputElement).value;
        this.updateForm(id, { text: val || undefined });
    }

    submitPrediction(id: number): void {
        const form = this.getForm(id);
        if (!form) return;

        this.updateForm(id, { saving: true, error: '', success: '' });

        const request: SubmitPredictionRequest = {
            weeklyPredictionId: form.weeklyPredictionId,
            leagueId: this.leagueId,
            targetType: form.targetType,
            driverId: form.driverId,
            teamId: form.teamId,
            text: form.text
        };

        this.predictionsService.submitPrediction(request).subscribe(result => {
            if (result.success) {
                this.updateForm(id, { saving: false, success: '✓ Saved!' });
                setTimeout(() => this.updateForm(id, { success: '' }), 2000);
            } else {
                this.updateForm(id, { saving: false, error: result.message });
            }
        });
    }

    isFormValid(id: number): boolean {
        const form = this.getForm(id);
        if (!form || !form.targetType) return false;
        if (form.targetType === 'Driver' && !form.driverId) return false;
        if (form.targetType === 'Team' && !form.teamId) return false;
        if (form.targetType === 'Text' && !form.text?.trim()) return false;
        return true;
    }

    private updateForm(id: number, updates: Partial<PredictionForm>): void {
        const forms = new Map(this.predictionForms());
        const current = forms.get(id);
        if (current) {
            forms.set(id, { ...current, ...updates });
            this.predictionForms.set(forms);
        }
    }

    // === VOTING ===

    castVote(predictionId: number, vote: boolean): void {
        this.hubService.castVote(predictionId, vote).subscribe(result => {
            if (result) {
                const feedback = new Map(this.votingFeedback());
                feedback.set(predictionId, vote ? 'Voted Yes ✓' : 'Voted No ✓');
                this.votingFeedback.set(feedback);

                // Refresh member predictions
                const race = this.hubService.currentRace();
                const member = this.selectedMember();
                if (race && member) {
                    this.hubService.getMemberPredictions(member.userId, race.id, this.leagueId).subscribe();
                }
            }
        });
    }

    getVoteFeedback(id: number): string {
        return this.votingFeedback().get(id) || '';
    }

    // === REPORTS ===

    openReportModal(predictionId: number): void {
        this.reportPredictionId.set(predictionId);
        this.reportReason.set('');
        this.showReportModal.set(true);
    }

    closeReportModal(): void {
        this.showReportModal.set(false);
    }

    submitReport(): void {
        this.reportSubmitting.set(true);
        this.hubService.reportPrediction(this.reportPredictionId(), this.reportReason()).subscribe(result => {
            this.reportSubmitting.set(false);
            if (result) {
                this.showReportModal.set(false);
            }
        });
    }

    // === HELPERS ===

    isSelf(member: MemberStanding): boolean {
        const user = this.authService.currentUser();
        return user !== null && member.userId === user.id;
    }

    getMemberInitials(member: MemberStanding): string {
        return (member.firstName[0] + member.lastName[0]).toUpperCase();
    }

    getRaceStateLabel(): string {
        const race = this.hubService.currentRace();
        if (!race) return '';
        switch (race.raceState) {
            case 'VotingOpen': return '🗳️ Voting Open';
            case 'VotingClosed': return '📊 Voting Closed';
            case 'Finalized': return '✅ Finalized';
            default:
                return race.arePredictionsLocked ? '🔒 Predictions Locked' : '📝 Predictions Open';
        }
    }

    getRaceStateClass(): string {
        const race = this.hubService.currentRace();
        if (!race) return '';
        if (race.raceState === 'VotingOpen') return 'state-voting';
        if (race.raceState === 'Finalized') return 'state-finalized';
        if (race.arePredictionsLocked) return 'state-locked';
        return 'state-open';
    }

    getPointsStatusIcon(status: string): string {
        switch (status) {
            case 'Correct': return '✅';
            case 'Wrong': return '❌';
            case 'VotingInProgress': return '🗳️';
            default: return '⏳';
        }
    }

    getPointsStatusClass(status: string): string {
        switch (status) {
            case 'Correct': return 'status-correct';
            case 'Wrong': return 'status-wrong';
            case 'VotingInProgress': return 'status-voting';
            default: return 'status-pending';
        }
    }

    getPredictionIcon(type: string): string {
        const icons: Record<string, string> = {
            'Pole': '🏎️', 'P1': '🥇', 'P2': '🥈', 'P3': '🥉',
            'SprintPole': '⚡', 'SprintWinner': '🏃',
            'Surprise': '🎲', 'Flop': '📉', 'Crazy': '🤪', 'Custom': '✨'
        };
        return icons[type] || '📋';
    }

    getTimeUntilLock(): string {
        const race = this.hubService.currentRace();
        if (!race || race.arePredictionsLocked) return 'Locked';

        const lock = new Date(race.predictionsLockedAt);
        const now = new Date();
        const diff = lock.getTime() - now.getTime();

        if (diff <= 0) return 'Locked';

        const hours = Math.floor(diff / 3600000);
        const minutes = Math.floor((diff % 3600000) / 60000);

        if (hours > 24) return `${Math.floor(hours / 24)}d ${hours % 24}h`;
        if (hours > 0) return `${hours}h ${minutes}m`;
        return `${minutes}m`;
    }
}
