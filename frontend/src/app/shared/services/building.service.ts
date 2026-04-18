import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { BuildingDetailsType } from "@shared/types/BuildingDetailsType";
import { BuildingType } from "@shared/types/BuildingType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { firstValueFrom } from "rxjs";

@Injectable({
    providedIn: "root",
})
export class BuildingService {
    private readonly httpClient: HttpClient = inject(HttpClient);

    public async downloadCsvReport(id: number): Promise<{ fileName: string; blob: Blob }> {
        const response = await firstValueFrom(
            this.httpClient.get(`${environment.serverURL}/api/buildings/${id}/csv-report`, {
                headers: this.createAuthorizationHeaders(),
                observe: "response",
                responseType: "blob",
            }),
        );

        const fallbackFileName: string = `building-${id}-report.csv`;
        const fileName: string = this.extractFileName(response.headers.get("content-disposition"), fallbackFileName);

        return {
            fileName,
            blob: response.body ?? new Blob([], { type: "text/csv;charset=utf-8" }),
        };
    }

    public async getPaged(page: number = 1, pageSize: number = 20): Promise<PagedResultType<BuildingType>> {
        const normalizedPage: number = Math.max(1, Math.trunc(page));
        const normalizedPageSize: number = Math.max(1, Math.trunc(pageSize));

        const params: HttpParams = new HttpParams()
            .set("page", normalizedPage)
            .set("pageSize", normalizedPageSize);

        return await firstValueFrom(
            this.httpClient.get<PagedResultType<BuildingType>>(`${environment.serverURL}/api/buildings`, {
                params,
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }

    public async getById(id: number): Promise<BuildingDetailsType> {
        return await firstValueFrom(
            this.httpClient.get<BuildingDetailsType>(`${environment.serverURL}/api/buildings/${id}`, {
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }
    
    public async searchByAddress(address: string, take: number = 5): Promise<BuildingType[]> {
        const params: HttpParams = new HttpParams()
            .set("address", address)
            .set("take", Math.min(10, Math.max(1, Math.trunc(take))));

        return await firstValueFrom(
            this.httpClient.get<BuildingType[]>(`${environment.serverURL}/api/buildings/search-by-address`, {
                params,
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

    private extractFileName(contentDisposition: string | null, fallback: string): string {
        if (!contentDisposition) {
            return fallback;
        }

        const utfMatch: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
        if (utfMatch?.[1]) {
            return decodeURIComponent(utfMatch[1]);
        }

        const plainMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
        if (plainMatch?.[1]) {
            return plainMatch[1];
        }

        return fallback;
    }
}
