import { Component, input, InputSignal, model, ModelSignal, output, OutputEmitterRef } from "@angular/core";

type TextareaTheme = "primary" | "ghost";

@Component({
    selector: "ui-textarea",
    imports: [],
    templateUrl: "./textarea.html",
    styleUrls: ["./textarea.css"],
})
export class Textarea {
    public readonly value: ModelSignal<string> = model<string>("");

    public readonly name: InputSignal<string> = input<string>("");
    public readonly label: InputSignal<string> = input<string>("");
    public readonly placeholder: InputSignal<string> = input<string>("");
    public readonly hint: InputSignal<string> = input<string>("");
    public readonly error: InputSignal<string> = input<string>("");
    public readonly theme: InputSignal<TextareaTheme> = input<TextareaTheme>("primary");

    public readonly isDisabled: InputSignal<boolean> = input<boolean>(false);
    public readonly isFullWidth: InputSignal<boolean> = input<boolean>(false);
    public readonly maxLength: InputSignal<number | null> = input<number | null>(null);

    public readonly valueChange: OutputEmitterRef<string> = output<string>();
    public readonly textareaFocus: OutputEmitterRef<FocusEvent> = output<FocusEvent>();
    public readonly textareaBlur: OutputEmitterRef<FocusEvent> = output<FocusEvent>();

    public handleInput(event: Event): void {
        const target = event.target as HTMLTextAreaElement;
        this.valueChange.emit(target.value);
    }

    public handleFocus(event: FocusEvent): void {
        this.textareaFocus.emit(event);
    }

    public handleBlur(event: FocusEvent): void {
        this.textareaBlur.emit(event);
    }
}
