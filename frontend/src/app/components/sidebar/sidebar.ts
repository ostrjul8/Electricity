import { Component } from "@angular/core";
import { NavItem } from "./nav-item/nav-item";

@Component({
    selector: "app-sidebar",
    imports: [NavItem],
    templateUrl: "./sidebar.html",
    styleUrl: "./sidebar.css",
})
export class Sidebar {}
