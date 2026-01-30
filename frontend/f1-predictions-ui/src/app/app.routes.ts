import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { ScheduleComponent } from './pages/schedule/schedule.component';
import { ResultsComponent } from './pages/results/results.component';
import { PredictionsComponent } from './pages/predictions/predictions.component';
import { MoreComponent } from './pages/more/more.component';
import { LoginComponent } from './pages/login/login.component';
import { SignupComponent } from './pages/signup/signup.component';

export const routes: Routes = [
    { path: '', component: HomeComponent },
    { path: 'schedule', component: ScheduleComponent },
    { path: 'results', component: ResultsComponent },
    { path: 'predictions', component: PredictionsComponent },
    { path: 'more', component: MoreComponent },
    { path: 'login', component: LoginComponent },
    { path: 'signup', component: SignupComponent },
    { path: '**', redirectTo: '' }
];
