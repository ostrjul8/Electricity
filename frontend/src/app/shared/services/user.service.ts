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
