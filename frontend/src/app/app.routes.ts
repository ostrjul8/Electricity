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
            {
                path: "chats",
                loadComponent: () => import("@pages/chats/chats").then((m) => m.Chats),
            },
            {
                path: "map",
                loadComponent: () => import("@pages/map/map").then((m) => m.Map),
            },
            {
                path: "profile",
                loadComponent: () => import("@pages/profile/profile").then((m) => m.Profile),
            },
        ],
    },
];
