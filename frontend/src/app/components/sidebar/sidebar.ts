import { Component, inject, computed } from "@angular/core";
import { Button } from "@shared/components/button/button";
import { AuthService } from "@shared/services/auth.service";

@Component({
    selector: "app-sidebar",
    imports: [Button],
    templateUrl: "./sidebar.html",
    styleUrl: "./sidebar.css",
})
export class Sidebar {
    protected readonly showChats = computed(() => {
        const user = this.authService.user();
        return user ? this.canAccessChats(user.role) : false;
    });

    private readonly authService: AuthService = inject(AuthService);

    private canAccessChats(role: string): boolean {
        return role === "AuthorizedUser" || role === "Admin";
    }
}
