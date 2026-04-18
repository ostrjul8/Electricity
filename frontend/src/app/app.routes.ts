import { Routes } from "@angular/router";
import { authGuard } from "@shared/core/auth.guard";
import { adminGuard } from "@shared/core/admin.guard";

export const routes: Routes = [
    {
        path: "auth",
        loadComponent: () => import("@layouts/auth-layout/auth-layout").then((m) => m.AuthLayout),
        children: [
            {
                path: "login",
                loadComponent: () => import("@pages/login/login").then((m) => m.Login),
            },
            {
                path: "register",
                loadComponent: () => import("@pages/register/register").then((m) => m.Register),
            },
            {
                path: "",
                pathMatch: "full",
                redirectTo: "login",
            },
        ],
    },
    {
        path: "login",
        redirectTo: "auth/login",
        pathMatch: "full",
    },
    {
        path: "register",
        redirectTo: "auth/register",
        pathMatch: "full",
    },
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
                canActivate: [authGuard],
            },
            {
                path: "map",
                loadComponent: () => import("@pages/map/map").then((m) => m.Map),
            },
            {
                path: "map-anomalies",
                loadComponent: () => import("@pages/map/map").then((m) => m.Map),
                canActivate: [adminGuard],
                data: { anomaliesOnly: true },
            },
            {
                path: "profile",
                loadComponent: () => import("@pages/profile/profile").then((m) => m.Profile),
                canActivate: [authGuard],
            },
            {
                path: "",
                pathMatch: "full",
                redirectTo: "map",
            },
        ],
    },
    {
        path: "**",
        redirectTo: "",
    },
];
