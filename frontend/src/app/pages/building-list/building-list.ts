import { CommonModule } from "@angular/common";
import { Component, computed, inject, OnDestroy, OnInit, Signal, signal, viewChild, ViewChild } from "@angular/core";
import { BuildingDetailsPopup } from "@shared/components/building-details-popup/building-details-popup";
import { BuildingService } from "@shared/services/building.service";
import { UserService } from "@shared/services/user.service";
import { BuildingType } from "@shared/types/BuildingType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { Input } from "@shared/components/input/input";
import { Button } from "@shared/components/button/button";

@Component({
    selector: "app-building-list",
    imports: [CommonModule, BuildingDetailsPopup, Input, Button],
    templateUrl: "./building-list.html",
    styleUrl: "./building-list.css",
})
export class BuildingList implements OnInit, OnDestroy {
    private readonly buildingDetailsPopup: Signal<BuildingDetailsPopup> = viewChild.required(BuildingDetailsPopup);

    private readonly buildingService: BuildingService = inject(BuildingService);
    private readonly userService: UserService = inject(UserService);

    protected readonly page = signal<number>(1);
    protected readonly pageSize = signal<number>(12);
    protected readonly totalPages = signal<number>(1);
    protected readonly totalCount = signal<number>(0);
    protected readonly buildings = signal<BuildingType[]>([]);
    protected readonly favorites = signal<BuildingType[]>([]);
    protected readonly loading = signal<boolean>(false);
    protected readonly savingFavoriteIds = signal<Set<number>>(new Set<number>());
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly searchText = signal<string>("");
    protected readonly searchMode = signal<boolean>(false);
    protected readonly searchResults = signal<BuildingType[]>([]);
    protected readonly isLoadingMore = signal<boolean>(false);

    protected readonly favoriteIds = computed<Set<number>>(() => {
        return new Set<number>(this.favorites().map((building: BuildingType) => building.id));
    });

    protected readonly visibleBuildings = computed<BuildingType[]>(() => {
        if (this.searchMode()) {
            return this.searchResults();
        }

        return this.buildings();
    });

    protected readonly title = computed<string>(() => {
        return this.searchMode() ? "Результати пошуку" : "Список будівель";
    });

    protected readonly canLoadMore = computed<boolean>(() => {
        return !this.searchMode() && this.page() < this.totalPages();
    });

    private searchSuggestionsTimeoutId: ReturnType<typeof setTimeout> | null = null;
    private searchSuggestionsRequestId: number = 0;

    public ngOnInit(): void {
        this.loadPage();
        this.loadFavorites();
    }

    public ngOnDestroy(): void {
        if (this.searchSuggestionsTimeoutId !== null) {
            clearTimeout(this.searchSuggestionsTimeoutId);
            this.searchSuggestionsTimeoutId = null;
        }
    }

    protected async loadPage(nextPage: number = this.page(), append: boolean = true): Promise<void> {
        if (append) {
            this.isLoadingMore.set(true);
        } else {
            this.loading.set(true);
        }

        this.errorMessage.set(null);

        try {
            const result: PagedResultType<BuildingType> = await this.buildingService.getPaged(
                nextPage,
                this.pageSize(),
            );

            this.page.set(result.page);
            this.pageSize.set(result.pageSize);
            this.totalPages.set(result.totalPages);
            this.totalCount.set(result.totalCount);

            if (append) {
                this.buildings.update((current: BuildingType[]) => [...current, ...result.items]);
            } else {
                this.buildings.set(result.items);
                this.searchMode.set(false);
                this.searchResults.set([]);
            }
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити список будівель."));
        } finally {
            if (append) {
                this.isLoadingMore.set(false);
            } else {
                this.loading.set(false);
            }
        }
    }

    protected async loadFavorites(): Promise<void> {
        try {
            const favorites: BuildingType[] = await this.userService.getFavorites();
            this.favorites.set(favorites);
        } catch {
            this.favorites.set([]);
        }
    }

    protected async loadMore(): Promise<void> {
        if (this.loading() || this.isLoadingMore() || !this.canLoadMore()) {
            return;
        }

        const nextPage: number = this.page() + 1;
        await this.loadPage(nextPage, true);
    }

    protected async handleSearchInput(value: string): Promise<void> {
        this.searchText.set(value);

        if (this.searchSuggestionsTimeoutId !== null) {
            clearTimeout(this.searchSuggestionsTimeoutId);
        }

        const query: string = value.trim();

        this.searchSuggestionsTimeoutId = setTimeout(async () => {
            if (query.length < 2) {
                this.searchMode.set(false);
                this.searchResults.set([]);
                await this.loadPage(1, false);
                return;
            }

            this.searchMode.set(true);
            this.loading.set(true);
            this.errorMessage.set(null);

            const requestId: number = ++this.searchSuggestionsRequestId;

            this.loadSearchSuggestions(query, requestId);
        }, 300);
    }

    private async loadSearchSuggestions(query: string, requestId: number): Promise<void> {
        try {
            const suggestions: BuildingType[] = await this.buildingService.searchByAddress(query, 10);

            if (requestId !== this.searchSuggestionsRequestId) {
                return;
            }

            this.searchResults.set(suggestions);
        } catch {
            this.searchResults.set([]);
        } finally {
            this.loading.set(false);
        }
    }

    protected async clearSearch(): Promise<void> {
        this.searchText.set("");
        this.searchMode.set(false);
        this.searchResults.set([]);
        this.errorMessage.set(null);
        await this.loadPage(1, false);
    }

    protected isFavorite(buildingId: number): boolean {
        return this.favoriteIds().has(buildingId);
    }

    protected isFavoriteSaving(buildingId: number): boolean {
        return this.savingFavoriteIds().has(buildingId);
    }

    protected async toggleFavorite(building: BuildingType, event: Event): Promise<void> {
        event.stopPropagation();
        event.preventDefault();

        const currentSaving: Set<number> = new Set<number>(this.savingFavoriteIds());
        currentSaving.add(building.id);
        this.savingFavoriteIds.set(currentSaving);

        try {
            if (this.isFavorite(building.id)) {
                await this.userService.removeFavorite(building.id);
                this.favorites.update((favorites: BuildingType[]) =>
                    favorites.filter((item: BuildingType) => item.id !== building.id),
                );
            } else {
                const addedFavorite: BuildingType = await this.userService.addFavorite(building.id);
                this.favorites.update((favorites: BuildingType[]) => {
                    if (favorites.some((item: BuildingType) => item.id === addedFavorite.id)) {
                        return favorites;
                    }

                    return [...favorites, addedFavorite];
                });
            }
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося оновити улюблені будівлі."));
        } finally {
            const nextSaving: Set<number> = new Set<number>(this.savingFavoriteIds());
            nextSaving.delete(building.id);
            this.savingFavoriteIds.set(nextSaving);
        }
    }

    protected openDetails(buildingId: number): void {
        this.buildingDetailsPopup().open(buildingId);
    }

    private getReadableError(error: unknown, fallbackMessage: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallbackMessage;
    }
}
