import { Component, HostListener, InputSignal, ModelSignal, OutputEmitterRef, input, model, output } from "@angular/core";

export type PopupSize = "small" | "medium" | "large";
export type PopupCloseReason = "backdrop" | "escape" | "button";

@Component({
    selector: "ui-popup",
    imports: [],
    templateUrl: "./popup.html",
    styleUrl: "./popup.css",
})
export class Popup {
    private static nextHeadingId: number = 1;

    public readonly isOpen: ModelSignal<boolean> = model<boolean>(false);
    public readonly title: InputSignal<string> = input<string>("");
    public readonly closeButtonLabel: InputSignal<string> = input<string>("Закрити");

    public readonly size: InputSignal<PopupSize> = input<PopupSize>("medium");

    public readonly closeOnBackdrop: InputSignal<boolean> = input<boolean>(true);
    public readonly closeOnEscape: InputSignal<boolean> = input<boolean>(true);
    public readonly showCloseButton: InputSignal<boolean> = input<boolean>(true);

    public readonly closed: OutputEmitterRef<PopupCloseReason> = output<PopupCloseReason>();

    public readonly headingId: string = `ui-popup-title-${Popup.nextHeadingId++}`;

    public close(reason: PopupCloseReason = "button"): void {
        if (!this.isOpen()) {
            return;
        }

        this.isOpen.set(false);
        this.closed.emit(reason);
    }

    public handleBackdropClick(event: MouseEvent): void {
        if (!this.closeOnBackdrop()) {
            return;
        }

        if (event.currentTarget !== event.target) {
            return;
        }

        this.close("backdrop");
    }

    public handleCloseButtonClick(event: MouseEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.close("button");
    }

    @HostListener("document:keydown.escape", ["$event"])
    public handleEscapeKey(event: Event): void {
        if (!(event instanceof KeyboardEvent)) {
            return;
        }

        if (!this.isOpen() || !this.closeOnEscape()) {
            return;
        }

        event.preventDefault();
        this.close("escape");
    }
}