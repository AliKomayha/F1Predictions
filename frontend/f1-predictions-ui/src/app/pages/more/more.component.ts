import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-more',
    standalone: true,
    imports: [CommonModule, RouterLink],
    templateUrl: './more.component.html',
    styleUrl: './more.component.scss'
})
export class MoreComponent {
    private authService = inject(AuthService);
    private router = inject(Router);

    currentUser = this.authService.currentUser;
    isLoggedIn = this.authService.isLoggedIn;

    logout(): void {
        this.authService.logout().subscribe(() => {
            this.router.navigate(['/login']);
        });
    }
}
