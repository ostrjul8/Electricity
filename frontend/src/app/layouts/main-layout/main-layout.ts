import { Component, computed, inject, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { NavigationEnd, Router, Event, RouterOutlet } from "@angular/router";
import { Sidebar } from "@components/sidebar/sidebar";
import { FloatingChat } from "@shared/components/floating-chat/floating-chat";
import { filter, map } from "rxjs";

@Component({
    selector: "app-main-layout",
    imports: [Sidebar, RouterOutlet, FloatingChat],
    templateUrl: "./main-layout.html",
    styleUrl: "./main-layout.css",
})
export class MainLayout {
    private readonly router: Router = inject(Router);

    private currentUrl = toSignal(
        this.router.events.pipe(
            filter((event: Event): event is NavigationEnd => event instanceof NavigationEnd),
            map((event) => event.urlAfterRedirects),
        ),
        { initialValue: this.router.url },
    );

    protected readonly showChatButton: Signal<boolean> = computed(() => {
        const url = this.currentUrl();

        return !url?.includes("/chats");
    });
}
