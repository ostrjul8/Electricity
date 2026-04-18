import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { firstValueFrom } from "rxjs";

export const authGuard: CanActivateFn = async () => {
    const router: Router = inject(Router);

    // if (authService.isLoggedIn()) {
    //     return true;
    // }

    const email: string | null = sessionStorage.getItem("email");
    const password: string | null = sessionStorage.getItem("password");

    if (email && password) {
        // return await firstValueFrom(authService.login(email, password))
        //     .then(() => {
        //         return true;
        //     })
        //     .catch(() => {
        //         router.navigate(["/login"]);
        //         return false;
        //     });
    }

    router.navigate(["/login"]);
    return false;
};
