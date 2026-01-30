import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss'
})
export class LoginComponent {
    phone = '';
    password = '';
    errorMessage = signal<string>('');
    isLoading = signal<boolean>(false);

    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    onSubmit(): void {
        if (!this.phone || !this.password) {
            this.errorMessage.set('Please fill in all fields');
            return;
        }

        this.isLoading.set(true);
        this.errorMessage.set('');

        this.authService.login({ phone: this.phone, password: this.password })
            .subscribe(response => {
                this.isLoading.set(false);
                if (response.success) {
                    this.router.navigate(['/']);
                } else {
                    this.errorMessage.set(response.message);
                }
            });
    }
}
