import { HttpClient, HttpHeaders } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { BuildingType } from "@shared/types/BuildingType";
import { UserListItemType } from "@shared/types/UserListItemType";
import { firstValueFrom } from "rxjs";

@Injectable({
    providedIn: "root",
})
export class UserService {
    private readonly httpClient: HttpClient = inject(HttpClient);

    public async getUsers(): Promise<UserListItemType[]> {
        return await firstValueFrom(
            this.httpClient.get<UserListItemType[]>(`${environment.serverURL}/api/auth/users`, {
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }

    public async getFavorites(): Promise<BuildingType[]> {
        return await firstValueFrom(
            this.httpClient.get<BuildingType[]>(`${environment.serverURL}/api/favorites`, {
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }

    public async addFavorite(buildingId: number): Promise<BuildingType> {
        return await firstValueFrom(
            this.httpClient.post<BuildingType>(
                `${environment.serverURL}/api/favorites`,
                { buildingId },
                { headers: this.createAuthorizationHeaders() },
            ),
        );
    }

    public async removeFavorite(buildingId: number): Promise<void> {
        await firstValueFrom(
            this.httpClient.delete<void>(`${environment.serverURL}/api/favorites/${buildingId}`, {
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }

    private createAuthorizationHeaders(): HttpHeaders {
        const accessToken: string | null = sessionStorage.getItem("accessToken");

        if (!accessToken) {
            return new HttpHeaders();
        }

        return new HttpHeaders({
            Authorization: `Bearer ${accessToken}`,
        });
    }
}
