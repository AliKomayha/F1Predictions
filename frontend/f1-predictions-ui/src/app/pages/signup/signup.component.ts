import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-signup',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    templateUrl: './signup.component.html',
    styleUrl: './signup.component.scss'
})
export class SignupComponent {
    firstName = '';
    lastName = '';
    phone = '';
    email = '';
    password = '';
    confirmPassword = '';

    errorMessage = signal<string>('');
    successMessage = signal<string>('');
    isLoading = signal<boolean>(false);
    showOtpForm = signal<boolean>(false);
    otpCode = '';

    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    onSubmit(): void {
        // Validate form
        if (!this.firstName || !this.lastName || !this.phone || !this.password || !this.confirmPassword) {
            this.errorMessage.set('Please fill in all required fields');
            return;
        }

        if (this.password !== this.confirmPassword) {
            this.errorMessage.set('Passwords do not match');
            return;
        }

        if (this.password.length < 6) {
            this.errorMessage.set('Password must be at least 6 characters');
            return;
        }

        this.isLoading.set(true);
        this.errorMessage.set('');

        this.authService.signup({
            firstName: this.firstName,
            lastName: this.lastName,
            phone: this.phone,
            email: this.email || undefined,
            password: this.password,
            confirmPassword: this.confirmPassword
        }).subscribe(response => {
            this.isLoading.set(false);
            if (response.success) {
                this.successMessage.set('Account created! Please verify your phone number.');
                this.showOtpForm.set(true);
            } else {
                this.errorMessage.set(response.message);
            }
        });
    }

    onVerifyOtp(): void {
        if (!this.otpCode || this.otpCode.length !== 6) {
            this.errorMessage.set('Please enter a valid 6-digit code');
            return;
        }

        this.isLoading.set(true);
        this.errorMessage.set('');

        this.authService.verifyPhone({
            phone: this.phone,
            code: this.otpCode
        }).subscribe(response => {
            this.isLoading.set(false);
            if (response.success) {
                this.router.navigate(['/']);
            } else {
                this.errorMessage.set(response.message);
            }
        });
    }
}
