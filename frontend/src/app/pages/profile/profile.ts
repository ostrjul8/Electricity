import { CommonModule } from "@angular/common";
import { Component, inject, OnInit, Signal, signal, viewChild } from "@angular/core";
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
    protected readonly saving = signal<boolean>(false);
    protected readonly editMode = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);

    protected readonly formName = signal<string>("");
    protected readonly formSurname = signal<string>("");
    protected readonly formPhone = signal<string>("");
    protected readonly formEmail = signal<string>("");
    
    private readonly buildingDetailsPopup: Signal<BuildingDetailsPopup> = viewChild.required(BuildingDetailsPopup);

    private readonly authService: AuthService = inject(AuthService);
    private readonly userService: UserService = inject(UserService);

    public ngOnInit(): void {
        this.loadProfile();
        this.loadFavorites();
    }

    protected startEdit(): void {
        const currentUser: UserType | null = this.user();

        if (!currentUser) {
            return;
        }

        this.formName.set(currentUser.name);
        this.formSurname.set(currentUser.surname);
        this.formPhone.set(currentUser.phone);
        this.formEmail.set(currentUser.email ?? "");

        this.errorMessage.set(null);
        this.editMode.set(true);
    }

    protected cancelEdit(): void {
        this.editMode.set(false);
        this.errorMessage.set(null);
    }

    protected handleNameInput(event: Event): void {
        this.formName.set((event.target as HTMLInputElement).value);
    }

    protected handleSurnameInput(event: Event): void {
        this.formSurname.set((event.target as HTMLInputElement).value);
    }

    protected handlePhoneInput(event: Event): void {
        this.formPhone.set((event.target as HTMLInputElement).value);
    }

    protected handleEmailInput(event: Event): void {
        this.formEmail.set((event.target as HTMLInputElement).value);
    }

    protected async saveProfile(): Promise<void> {
        const name: string = this.formName().trim();
        const surname: string = this.formSurname().trim();
        const phone: string = this.formPhone().trim();
        const email: string = this.formEmail().trim();

        if (!name || !surname || !phone) {
            this.errorMessage.set("Ім'я, прізвище та телефон є обов'язковими.");
            return;
        }

        this.saving.set(true);
        this.errorMessage.set(null);

        try {
            const updatedUser: UserType = await this.authService.updateProfile({
                name,
                surname,
                phone,
                email,
            });

            this.user.set(updatedUser);
            this.editMode.set(false);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося оновити профіль."));
        } finally {
            this.saving.set(false);
        }
    }

    protected async removeFavorite(buildingId: number): Promise<void> {
        try {
            await this.userService.removeFavorite(buildingId);
            this.favorites.update((items: BuildingType[]) => items.filter((item: BuildingType) => item.id !== buildingId));
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося видалити будівлю з обраних."));
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
