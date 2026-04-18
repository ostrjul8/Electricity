import { CommonModule } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Button } from "@shared/components/button/button";
import { AuthService } from "@shared/services/auth.service";

@Component({
    selector: "app-login",
    imports: [CommonModule, Button, RouterLink],
    templateUrl: "./login.html",
    styleUrl: "./login.css",
})
export class Login {
    protected readonly email = signal<string>("");
    protected readonly password = signal<string>("");
    protected readonly loading = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);

    private readonly authService: AuthService = inject(AuthService);
    private readonly router: Router = inject(Router);

    protected handleEmailInput(event: Event): void {
        this.email.set((event.target as HTMLInputElement).value);
    }

    protected handlePasswordInput(event: Event): void {
        this.password.set((event.target as HTMLInputElement).value);
    }

    protected async submit(): Promise<void> {
        const email: string = this.email().trim().toLowerCase();
        const password: string = this.password();

        if (!email || !password) {
            this.errorMessage.set("Введіть email та пароль.");
            return;
        }

        this.loading.set(true);
        this.errorMessage.set(null);

        try {
            await this.authService.login({ email, password });
            await this.router.navigateByUrl("/map");
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося увійти. Перевірте дані."));
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
