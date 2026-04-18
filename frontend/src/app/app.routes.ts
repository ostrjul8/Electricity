import { Routes } from "@angular/router";

export const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("@layouts/main-layout/main-layout").then((m) => m.MainLayout),
        children: [
            {
                path: "building-list",
                loadComponent: () => import("@pages/building-list/building-list").then((m) => m.BuildingList),
            },
        ],
    },
];
