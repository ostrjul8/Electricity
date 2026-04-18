import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { AuthService } from "@shared/services/auth.service";

export const adminGuard: CanActivateFn = async () => {
    const authService: AuthService = inject(AuthService);
    const router: Router = inject(Router);

    if (!authService.isLoggedIn()) {
        await router.navigate(["/auth/login"]);
        return false;
    }

    try {
        const user = authService.user() ?? await authService.getAuthorizedUser();

        if (user.role === "Admin") {
            return true;
        }

        await router.navigate(["/map"]);
        return false;
    } catch {
        await authService.logout();
        await router.navigate(["/auth/login"]);
        return false;
    }
};