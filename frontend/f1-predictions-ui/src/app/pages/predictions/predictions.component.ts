import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PredictionsService } from '../../services/predictions.service';
import { LeaguesService } from '../../services/leagues.service';
import { AuthService } from '../../services/auth.service';
import { League, LeagueMember } from '../../models/league.model';
import {
    RacePrediction,
    RaceOption,
    DriverOption,
    TeamOption,
    SubmitPredictionRequest
} from '../../models/prediction.model';

type ViewMode = 'members' | 'view-predictions' | 'make-predictions';

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
    selector: 'app-predictions',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    templateUrl: './predictions.component.html',
    styleUrl: './predictions.component.scss'
})
export class PredictionsComponent implements OnInit {
    // Navigation state
    viewMode = signal<ViewMode>('members');
    selectedLeagueId = signal<number | null>(null);
    selectedRaceId = signal<number | null>(null);
    selectedMember = signal<LeagueMember | null>(null);

    // Computed
    selectedLeague = signal<League | null>(null);
    selectedRace = signal<RaceOption | null>(null);

    // Per-prediction form state (for make-predictions mode)
    predictionForms = signal<Map<number, PredictionForm>>(new Map());

    // Helper to check if viewing own predictions
    isViewingSelf = computed(() => {
        const member = this.selectedMember();
        const user = this.authService.currentUser();
        return member && user && member.userId === user.id;
    });

    constructor(
        public predictionsService: PredictionsService,
        public leaguesService: LeaguesService,
        public authService: AuthService
    ) { }

    ngOnInit(): void {
        if (this.authService.isLoggedIn()) {
            this.leaguesService.getUserLeagues().subscribe();
        }
    }

    // === LEAGUE SELECTION ===
    onLeagueChange(event: Event): void {
        const select = event.target as HTMLSelectElement;
        const leagueId = select.value ? parseInt(select.value) : null;
        this.selectedLeagueId.set(leagueId);
        this.resetToMembers();

        if (leagueId) {
            const league = this.leaguesService.leagues().find(l => l.id === leagueId);
            this.selectedLeague.set(league || null);
            this.leaguesService.getLeagueMembers(leagueId).subscribe();
            this.predictionsService.getRacesForLeague(leagueId).subscribe();
        } else {
            this.selectedLeague.set(null);
            this.leaguesService.clearMembers();
        }
    }

    // === MEMBER CLICK ===
    onMemberClick(member: LeagueMember): void {
        this.selectedMember.set(member);
        this.selectedRaceId.set(null);
        this.selectedRace.set(null);
        this.predictionsService.clearPredictions();

        const user = this.authService.currentUser();
        if (user && member.userId === user.id) {
            // Clicking on yourself → make predictions mode
            this.viewMode.set('make-predictions');
        } else {
            // Clicking on another member → view their predictions (read-only)
            this.viewMode.set('view-predictions');
        }
    }

    // === MAKE PREDICTION BUTTON ===
    onMakePredictionClick(): void {
        const user = this.authService.currentUser();
        if (user) {
            const selfMember: LeagueMember = {
                userId: user.id,
                firstName: user.firstName,
                lastName: user.lastName,
                role: 'Member',
                joinedAt: new Date()
            };
            this.selectedMember.set(selfMember);
            this.selectedRaceId.set(null);
            this.selectedRace.set(null);
            this.predictionsService.clearPredictions();
            this.viewMode.set('make-predictions');
        }
    }

    // === BACK TO MEMBERS ===
    goBackToMembers(): void {
        this.resetToMembers();
    }

    private resetToMembers(): void {
        this.viewMode.set('members');
        this.selectedMember.set(null);
        this.selectedRaceId.set(null);
        this.selectedRace.set(null);
        this.predictionsService.clearPredictions();
        this.predictionForms.set(new Map());
    }

    // === RACE SELECTION ===
    onRaceChange(event: Event): void {
        const select = event.target as HTMLSelectElement;
        const raceId = select.value ? parseInt(select.value) : null;
        this.selectedRaceId.set(raceId);
        this.predictionsService.clearPredictions();
        this.predictionForms.set(new Map());

        if (raceId) {
            const race = this.predictionsService.races().find(r => r.id === raceId);
            this.selectedRace.set(race || null);
            this.loadPredictionsForMode(raceId);
        } else {
            this.selectedRace.set(null);
        }
    }

    private loadPredictionsForMode(raceId: number): void {
        const leagueId = this.selectedLeagueId();
        const member = this.selectedMember();
        if (!leagueId || !member) return;

        if (this.viewMode() === 'make-predictions') {
            // Load own predictions with driver/team options
            forkJoin({
                predictions: this.predictionsService.getRacePredictions(raceId, leagueId),
                drivers: this.predictionsService.getDriversForRace(raceId),
                teams: this.predictionsService.getTeamsForRace(raceId)
            }).subscribe(({ predictions }) => {
                this.initForms(predictions);
            });
        } else {
            // Load member's predictions (read-only)
            this.predictionsService.getMemberPredictions(member.userId, raceId, leagueId).subscribe();
        }
    }

    private initForms(predictions: RacePrediction[]): void {
        const forms = new Map<number, PredictionForm>();
        for (const p of predictions) {
            const allowedTypes = p.allowedTargetTypes.split(',').map(s => s.trim());
            forms.set(p.weeklyPredictionId, {
                weeklyPredictionId: p.weeklyPredictionId,
                targetType: p.userPick?.targetType || allowedTypes[0],
                driverId: p.userPick?.driverId,
                teamId: p.userPick?.teamId,
                text: p.userPick?.text || '',
                saving: false,
                success: '',
                error: ''
            });
        }
        this.predictionForms.set(forms);
    }

    // === FORM HELPERS ===
    getAllowedTypes(prediction: RacePrediction): string[] {
        return prediction.allowedTargetTypes.split(',').map(s => s.trim());
    }

    getForm(predictionId: number): PredictionForm | undefined {
        return this.predictionForms().get(predictionId);
    }

    onTargetTypeChange(predictionId: number, targetType: string): void {
        const forms = new Map(this.predictionForms());
        const form = forms.get(predictionId);
        if (form) {
            forms.set(predictionId, { ...form, targetType, driverId: undefined, teamId: undefined, text: '' });
            this.predictionForms.set(forms);
        }
    }

    onDriverChange(predictionId: number, event: Event): void {
        const select = event.target as HTMLSelectElement;
        const forms = new Map(this.predictionForms());
        const form = forms.get(predictionId);
        if (form) {
            forms.set(predictionId, { ...form, driverId: select.value ? parseInt(select.value) : undefined });
            this.predictionForms.set(forms);
        }
    }

    onTeamChange(predictionId: number, event: Event): void {
        const select = event.target as HTMLSelectElement;
        const forms = new Map(this.predictionForms());
        const form = forms.get(predictionId);
        if (form) {
            forms.set(predictionId, { ...form, teamId: select.value ? parseInt(select.value) : undefined });
            this.predictionForms.set(forms);
        }
    }

    onTextChange(predictionId: number, event: Event): void {
        const input = event.target as HTMLTextAreaElement;
        const forms = new Map(this.predictionForms());
        const form = forms.get(predictionId);
        if (form) {
            forms.set(predictionId, { ...form, text: input.value });
            this.predictionForms.set(forms);
        }
    }

    // === SUBMIT ===
    submitPrediction(predictionId: number): void {
        const form = this.predictionForms().get(predictionId);
        const leagueId = this.selectedLeagueId();
        if (!form || !leagueId) return;

        this.updateFormState(predictionId, { saving: true, success: '', error: '' });

        const request: SubmitPredictionRequest = {
            weeklyPredictionId: form.weeklyPredictionId,
            leagueId: leagueId,
            targetType: form.targetType,
            driverId: form.targetType === 'Driver' ? form.driverId : undefined,
            teamId: form.targetType === 'Team' ? form.teamId : undefined,
            text: form.targetType === 'Text' ? form.text : undefined
        };

        this.predictionsService.submitPrediction(request).subscribe(result => {
            if (result.success) {
                this.updateFormState(predictionId, { saving: false, success: result.message, error: '' });
                const raceId = this.selectedRaceId();
                if (raceId) {
                    this.predictionsService.getRacePredictions(raceId, leagueId).subscribe();
                }
                setTimeout(() => this.updateFormState(predictionId, { success: '' }), 3000);
            } else {
                this.updateFormState(predictionId, { saving: false, success: '', error: result.message });
            }
        });
    }

    private updateFormState(predictionId: number, updates: Partial<PredictionForm>): void {
        const forms = new Map(this.predictionForms());
        const form = forms.get(predictionId);
        if (form) {
            forms.set(predictionId, { ...form, ...updates });
            this.predictionForms.set(forms);
        }
    }

    // === DISPLAY HELPERS ===
    getPointsForType(targetType: string): number {
        switch (targetType) {
            case 'Driver': return 1;
            case 'Team': return 2;
            case 'Text': return 1;
            default: return 0;
        }
    }

    getPredictionIcon(type: string): string {
        switch (type) {
            case 'Pole': return '🏁';
            case 'P1': return '🥇';
            case 'P2': return '🥈';
            case 'P3': return '🥉';
            case 'SprintPole': return '⚡';
            case 'SprintWinner': return '🏃';
            case 'Surprise': return '🎯';
            case 'Flop': return '📉';
            case 'Crazy': return '🤪';
            case 'Custom': return '✏️';
            default: return '🔮';
        }
    }

    getMemberInitials(member: LeagueMember): string {
        return (member.firstName[0] + member.lastName[0]).toUpperCase();
    }

    isSelf(member: LeagueMember): boolean {
        const user = this.authService.currentUser();
        return !!user && member.userId === user.id;
    }

    isFormValid(predictionId: number): boolean {
        const form = this.predictionForms().get(predictionId);
        if (!form) return false;

        switch (form.targetType) {
            case 'Driver': return !!form.driverId;
            case 'Team': return !!form.teamId;
            case 'Text': return !!form.text?.trim();
            default: return false;
        }
    }

    hasPick(prediction: RacePrediction): boolean {
        return !!prediction.userPick;
    }
}
