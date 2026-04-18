import { Component, input, InputSignal, output, OutputEmitterRef } from "@angular/core";
import { RouterLink } from "@angular/router";

type ButtonTheme = "primary" | "secondary" | "danger";
type ButtonSize = "small" | "large";
type ButtonType = "default" | "underline" | "outline" | "empty";
type ButtonButtonType = "button" | "submit" | "reset";

@Component({
    selector: "ui-button",
    imports: [RouterLink],
    templateUrl: "./button.html",
    styleUrl: "./button.css",
})
export class Button {
    public readonly ariaLabel: InputSignal<string> = input<string>("");
    public readonly label: InputSignal<string> = input<string>("");
    public readonly icon: InputSignal<string> = input<string>("");

    public readonly buttonType: InputSignal<ButtonButtonType> = input<ButtonButtonType>("button");
    public readonly size: InputSignal<ButtonSize> = input<ButtonSize>("small");
    public readonly theme: InputSignal<ButtonTheme> = input<ButtonTheme>("primary");
    public readonly type: InputSignal<ButtonType> = input<ButtonType>("default");

    public readonly isDisabled: InputSignal<boolean> = input<boolean>(false);
    public readonly isFullWidth: InputSignal<boolean> = input<boolean>(false);
    public readonly loading: InputSignal<boolean> = input<boolean>(false);

    public readonly routerLinkValue: InputSignal<string | unknown[] | null> = input<string | unknown[] | null>(null);

    public readonly buttonClick: OutputEmitterRef<MouseEvent> = output<MouseEvent>();

    public handleClick(event: MouseEvent): void {
        if (this.isDisabled() || this.loading()) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.buttonClick.emit(event);
    }
}
