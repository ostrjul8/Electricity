import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { AuthService } from "@shared/services/auth.service";

export const authGuard: CanActivateFn = async () => {
    const authService: AuthService = inject(AuthService);
    const router: Router = inject(Router);

    if (authService.isLoggedIn()) {
        return true;
    }

    const refreshToken: string | null = localStorage.getItem("refreshToken");

    if (refreshToken) {
        return await firstValueFrom(authService.refreshToken())
            .then(async () => {
                await authService.getAuthorizedUser();
                return true;
            })
            .catch(async () => {
                await authService.logout();
                await router.navigate(["/auth/login"]);
                return false;
            });
    }

    await router.navigate(["/auth/login"]);
    return false;
};
