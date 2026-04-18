import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal, WritableSignal } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { UserType } from "@shared/types/UserType";
import { firstValueFrom, Observable, switchMap } from "rxjs";

@Injectable({
    providedIn: "root",
})
export class AuthService {
    private readonly profileStorageKey: string = "userProfile";

    public readonly user: WritableSignal<UserType | null> =
        signal<UserType | null>(null);

    private readonly httpClient: HttpClient = inject(HttpClient);

    public async signUp(
        user: Omit<UserType, "id"> & { password: string },
    ): Promise<{ message: string }> {
        return await firstValueFrom(
            this.httpClient.post<{ accessToken: string; refreshToken: string }>(
                environment.serverURL + "/api/auth/register",
                user,
            ),
        ).then((response: { accessToken: string; refreshToken: string }) => {
            sessionStorage.setItem("accessToken", response.accessToken);
            localStorage.setItem("refreshToken", response.refreshToken);

            this.getAuthorizedUser();

            return { message: "User registered successfully" };
        });
    }

    public async login(credentials: {
        phone: string;
        password: string;
    }): Promise<{ message: string }> {
        return await firstValueFrom(
            this.httpClient.post<{ accessToken: string; refreshToken: string }>(
                environment.serverURL + "/api/auth/login",
                credentials,
            ),
        ).then((response: { accessToken: string; refreshToken: string }) => {
            sessionStorage.setItem("accessToken", response.accessToken);
            localStorage.setItem("refreshToken", response.refreshToken);

            this.getAuthorizedUser();

            return { message: "User logged in successfully" };
        });
    }

    public async getAuthorizedUser(): Promise<UserType> {
        return await Promise.resolve().then(async () => {
            const fallbackUser: UserType = {
                id: "1",
                name: "John",
                surname: "Doe",
                phone: "+1234567890",
                email: "john.doe@example.com",
            };

            const storedUser: UserType | null = this.readStoredProfile();
            const user: UserType = storedUser ?? fallbackUser;

            this.storeProfile(user);

            this.user.set(user);
            return user;
        });
    }

    public async updateProfile(profile: Omit<UserType, "id">): Promise<UserType> {
        const currentUser: UserType = this.user() ?? await this.getAuthorizedUser();

        const updatedUser: UserType = {
            ...currentUser,
            ...profile,
        };

        this.storeProfile(updatedUser);
        this.user.set(updatedUser);

        return updatedUser;
    }

    public async logout(): Promise<{ message: string }> {
        sessionStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem(this.profileStorageKey);

        this.user.set(null);

        return { message: "Logged out successfully" };
    }

    public refreshToken(): Observable<{ accessToken: string }> {
        const refreshToken: string | null =
            localStorage.getItem("refreshToken");

        const refreshObservable: Observable<{ accessToken: string }> =
            this.httpClient.post<{ accessToken: string }>(
                environment.serverURL + "/api/auth/refresh",
                { refreshToken },
            );

        refreshObservable.subscribe((response: { accessToken: string }) => {
            sessionStorage.setItem("accessToken", response.accessToken);
        });

        return refreshObservable;
    }

    public async init(): Promise<void> {
        await this.getAuthorizedUser().catch(() => {
            this.user.set(null);
        });
    }

    private storeProfile(profile: UserType): void {
        localStorage.setItem(this.profileStorageKey, JSON.stringify(profile));
    }

    private readStoredProfile(): UserType | null {
        const rawProfile: string | null = localStorage.getItem(this.profileStorageKey);

        if (!rawProfile) {
            return null;
        }

        try {
            const parsed: unknown = JSON.parse(rawProfile);

            if (!parsed || typeof parsed !== "object") {
                return null;
            }

            const candidate: Partial<UserType> = parsed as Partial<UserType>;

            if (
                typeof candidate.id !== "string"
                || typeof candidate.name !== "string"
                || typeof candidate.surname !== "string"
                || typeof candidate.phone !== "string"
            ) {
                return null;
            }

            return {
                id: candidate.id,
                name: candidate.name,
                surname: candidate.surname,
                phone: candidate.phone,
                email: typeof candidate.email === "string" ? candidate.email : "",
            };
        } catch {
            return null;
        }
    }
}
