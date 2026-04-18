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
    private readonly useMockSearchResults: boolean = true;
    private readonly mockSearchResults: BuildingType[] = this.createMockSearchResults();

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
        if (this.useMockSearchResults) {
            return this.searchMockBuildings(address, take);
        }

        const params: HttpParams = new HttpParams()
            .set("address", address)
            .set("take", Math.min(5, Math.max(1, Math.trunc(take))));

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

    private searchMockBuildings(address: string, take: number): BuildingType[] {
        const query: string = address.trim().toLocaleLowerCase();
        const limit: number = Math.min(5, Math.max(1, Math.trunc(take)));

        if (!query) {
            return [];
        }

        return this.mockSearchResults
            .filter((building: BuildingType) => {
                const buildingAddress: string = building.address.toLocaleLowerCase();
                const buildingName: string = building.name.toLocaleLowerCase();

                return buildingAddress.includes(query) || buildingName.includes(query);
            })
            .slice(0, limit);
    }

    private createMockSearchResults(): BuildingType[] {
        return [
            this.createMockBuilding(1001, "ЖК Лісовий", "вул. Братиславська, 14", "Житловий будинок", "Цегла", 25, 4200),
            this.createMockBuilding(1002, "Будинок на Лесі Українки", "бул. Лесі Українки, 26", "Багатоквартирний будинок", "Моноліт", 18, 5100),
            this.createMockBuilding(1003, "Освітній корпус", "просп. Перемоги, 37", "Навчальний корпус", "Бетон", 9, 2800),
            this.createMockBuilding(1004, "Клініка Подолу", "вул. Набережно-Хрещатицька, 3", "Медичний заклад", "Цегла", 6, 7600),
            this.createMockBuilding(1005, "Офісний центр", "вул. Велика Васильківська, 72", "Офісна будівля", "Скло", 16, 6400),
            this.createMockBuilding(1006, "Садиба на Оболоні", "просп. Героїв Сталінграда, 8", "Житловий будинок", "Панель", 12, 3300),
        ];
    }

    private createMockBuilding(
        id: number,
        name: string,
        address: string,
        type: string,
        material: string,
        floors: number,
        averageConsumption: number,
    ): BuildingType {
        return {
            id,
            name,
            address,
            type,
            material,
            floors,
            averageConsumption,
            area: 0,
            longitude: 30.5234 + (id % 10) * 0.01,
            latitude: 50.4501 + (id % 10) * 0.01,
            districtId: (id % 10) + 1,
            districtName: `Район ${(id % 10) + 1}`,
        };
    }
}
