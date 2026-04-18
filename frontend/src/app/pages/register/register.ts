import { CommonModule } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Button } from "@shared/components/button/button";
import { AuthService } from "@shared/services/auth.service";

@Component({
    selector: "app-register",
    imports: [CommonModule, Button, RouterLink],
    templateUrl: "./register.html",
    styleUrl: "./register.css",
})
export class Register {
    protected readonly username = signal<string>("");
    protected readonly email = signal<string>("");
    protected readonly password = signal<string>("");
    protected readonly repeatPassword = signal<string>("");
    protected readonly loading = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);

    private readonly authService: AuthService = inject(AuthService);
    private readonly router: Router = inject(Router);

    protected handleUsernameInput(event: Event): void {
        this.username.set((event.target as HTMLInputElement).value);
    }

    protected handleEmailInput(event: Event): void {
        this.email.set((event.target as HTMLInputElement).value);
    }

    protected handlePasswordInput(event: Event): void {
        this.password.set((event.target as HTMLInputElement).value);
    }

    protected handleRepeatPasswordInput(event: Event): void {
        this.repeatPassword.set((event.target as HTMLInputElement).value);
    }

    protected async submit(): Promise<void> {
        const username: string = this.username().trim();
        const email: string = this.email().trim().toLowerCase();
        const password: string = this.password();
        const repeatPassword: string = this.repeatPassword();

        if (!username || !email || !password || !repeatPassword) {
            this.errorMessage.set("Заповніть усі поля.");
            return;
        }

        if (password !== repeatPassword) {
            this.errorMessage.set("Паролі не співпадають.");
            return;
        }

        this.loading.set(true);
        this.errorMessage.set(null);

        try {
            await this.authService.signUp({ username, email, password });
            await this.router.navigateByUrl("/map");
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося створити акаунт."));
        } finally {
            this.loading.set(false);
        }
    }

    private getReadableError(error: unknown, fallback: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallback;
    }
}
