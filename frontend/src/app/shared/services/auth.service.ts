import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal, WritableSignal } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { UserType } from "@shared/types/UserType";
import { firstValueFrom, Observable, tap, throwError } from "rxjs";

type BackendUserDto = {
    id: number;
    username: string;
    email: string;
    role: string;
};

type AuthResponse = {
    accessToken: string;
    refreshToken: string;
    expiresAtUtc: string;
    role: string;
    user: BackendUserDto;
};

type RegisterCredentials = {
    username: string;
    email: string;
    password: string;
};

type LoginCredentials = {
    email: string;
    password: string;
};

@Injectable({
    providedIn: "root",
})
export class AuthService {
    private readonly profileStorageKey: string = "userProfile";

    public readonly user: WritableSignal<UserType | null> =
        signal<UserType | null>(null);

    private readonly httpClient: HttpClient = inject(HttpClient);

    public async signUp(
        credentials: RegisterCredentials,
    ): Promise<{ message: string }> {
        return await firstValueFrom(
            this.httpClient.post<AuthResponse>(
                environment.serverURL + "/api/auth/register",
                credentials,
            ),
        ).then((response: AuthResponse) => {
            this.applyAuthResponse(response);

            return { message: "User registered successfully" };
        });
    }

    public async login(credentials: LoginCredentials): Promise<{ message: string }> {
        return await firstValueFrom(
            this.httpClient.post<AuthResponse>(
                environment.serverURL + "/api/auth/login",
                credentials,
            ),
        ).then((response: AuthResponse) => {
            this.applyAuthResponse(response);

            return { message: "User logged in successfully" };
        });
    }

    public async getAuthorizedUser(): Promise<UserType> {
        const currentUser: UserType | null = this.user();
        if (currentUser) {
            return currentUser;
        }

        const accessToken: string | null = sessionStorage.getItem("accessToken");
        if (!accessToken) {
            throw new Error("Користувач не авторизований.");
        }

        try {
            const backendUser: BackendUserDto = await firstValueFrom(
                this.httpClient.get<BackendUserDto>(environment.serverURL + "/api/auth/me"),
            );

            const mappedUser: UserType = this.mapBackendUser(backendUser);
            this.storeProfile(mappedUser);
            this.user.set(mappedUser);

            return mappedUser;
        } catch {
            const storedUser: UserType | null = this.readStoredProfile();
            if (storedUser) {
                this.user.set(storedUser);
                return storedUser;
            }

            throw new Error("Не вдалося відновити профіль користувача.");
        }
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

        if (!refreshToken) {
            return throwError(() => new Error("Refresh token missing."));
        }

        const refreshObservable: Observable<{ accessToken: string }> =
            this.httpClient.post<AuthResponse>(
                environment.serverURL + "/api/auth/refresh",
                { refreshToken },
            ).pipe(
                tap((response: AuthResponse) => {
                    this.applyAuthResponse(response);
                }),
            );

        return refreshObservable;
    }

    public async init(): Promise<void> {
        const token: string | null = sessionStorage.getItem("accessToken");
        const refreshToken: string | null = localStorage.getItem("refreshToken");

        if (!token && !refreshToken) {
            this.user.set(null);
            return;
        }

        if (!token && refreshToken) {
            try {
                await firstValueFrom(this.refreshToken());
            } catch {
                await this.logout();
                return;
            }
        }

        await this.getAuthorizedUser().catch(() => {
            this.user.set(null);
        });
    }

    public isLoggedIn(): boolean {
        return Boolean(sessionStorage.getItem("accessToken"));
    }

    private applyAuthResponse(response: AuthResponse): void {
        sessionStorage.setItem("accessToken", response.accessToken);
        localStorage.setItem("refreshToken", response.refreshToken);

        const mappedUser: UserType = this.mapBackendUser(response.user);
        this.storeProfile(mappedUser);
        this.user.set(mappedUser);
    }

    private mapBackendUser(user: BackendUserDto): UserType {
        return {
            id: String(user.id),
            name: user.username,
            surname: "",
            phone: "",
            email: user.email,
            role: user.role,
        };
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
                role: typeof candidate.role === "string" ? candidate.role : "",
            };
        } catch {
            return null;
        }
    }
}
