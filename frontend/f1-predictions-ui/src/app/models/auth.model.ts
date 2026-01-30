export interface User {
    id: number;
    firstName: string;
    lastName: string;
    phone: string;
    email?: string;
    createdAt: Date;
    isActive: boolean;
    isPhoneVerified: boolean;
    isEmailVerified: boolean;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    user?: User;
}

export interface SignupRequest {
    firstName: string;
    lastName: string;
    phone: string;
    email?: string;
    password: string;
    confirmPassword: string;
}

export interface LoginRequest {
    phone: string;
    password: string;
}

export interface VerifyPhoneRequest {
    phone: string;
    code: string;
}
