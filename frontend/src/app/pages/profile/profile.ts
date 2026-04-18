import { CommonModule } from "@angular/common";
import { Component, inject, OnInit, Signal, signal, viewChild } from "@angular/core";
import { Router } from "@angular/router";
import { BuildingDetailsPopup } from "@shared/components/building-details-popup/building-details-popup";
import { Button } from "@shared/components/button/button";
import { AuthService } from "@shared/services/auth.service";
import { UserService } from "@shared/services/user.service";
import { BuildingType } from "@shared/types/BuildingType";
import { UserType } from "@shared/types/UserType";

@Component({
    selector: "app-profile",
    imports: [CommonModule, Button, BuildingDetailsPopup],
    templateUrl: "./profile.html",
    styleUrl: "./profile.css",
})
export class Profile implements OnInit {
    protected readonly user = signal<UserType | null>(null);
    protected readonly favorites = signal<BuildingType[]>([]);
    protected readonly loading = signal<boolean>(true);
    protected readonly favoritesLoading = signal<boolean>(true);
    protected readonly loggingOut = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);
    
    private readonly buildingDetailsPopup: Signal<BuildingDetailsPopup> = viewChild.required(BuildingDetailsPopup);

    private readonly authService: AuthService = inject(AuthService);
    private readonly userService: UserService = inject(UserService);
    private readonly router: Router = inject(Router);

    public ngOnInit(): void {
        this.loadProfile();
        this.loadFavorites();
    }

    protected async removeFavorite(buildingId: number): Promise<void> {
        try {
            await this.userService.removeFavorite(buildingId);
            this.favorites.update((items: BuildingType[]) => items.filter((item: BuildingType) => item.id !== buildingId));
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося видалити будівлю з обраних."));
        }
    }

    protected async logout(): Promise<void> {
        this.loggingOut.set(true);
        this.errorMessage.set(null);

        try {
            await this.authService.logout();
            await this.router.navigate(["/auth/login"]);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося вийти з акаунта."));
        } finally {
            this.loggingOut.set(false);
        }
    }

    private async loadProfile(): Promise<void> {
        this.loading.set(true);
        this.errorMessage.set(null);

        try {
            const currentUser: UserType = this.authService.user() ?? await this.authService.getAuthorizedUser();
            this.user.set(currentUser);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити профіль."));
        } finally {
            this.loading.set(false);
        }
    }

    private async loadFavorites(): Promise<void> {
        this.favoritesLoading.set(true);

        try {
            const favorites: BuildingType[] = await this.userService.getFavorites();
            this.favorites.set(favorites);
        } catch {
            this.favorites.set([]);
        } finally {
            this.favoritesLoading.set(false);
        }
    }

    protected openDetails(buildingId: number): void {
        this.buildingDetailsPopup().open(buildingId);
    }

    private getReadableError(error: unknown, fallback: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallback;
    }
}
