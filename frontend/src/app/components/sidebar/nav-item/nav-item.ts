import { Component, input, InputSignal } from "@angular/core";
import { RouterLink } from "@angular/router";

@Component({
    selector: "app-nav-item",
    imports: [RouterLink],
    templateUrl: "./nav-item.html",
    styleUrl: "./nav-item.css",
})
export class NavItem {
    public readonly label: InputSignal<string> = input.required<string>();
    public readonly path: InputSignal<string> = input.required<string>();
    public readonly icon: InputSignal<string> = input.required<string>();
}
