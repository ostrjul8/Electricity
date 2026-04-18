import { Component, computed, input, InputSignal, model, ModelSignal, output, OutputEmitterRef } from "@angular/core";

type InputType = "text" | "number" | "phone" | "password";
type InputInputType = "text" | "tel" | "password";
type InputSize = "small" | "large";
type InputTheme = "default" | "ghost";

@Component({
    selector: "ui-input",
    imports: [],
    templateUrl: "./input.html",
    styleUrl: "./input.css",
})
export class Input {
    public readonly value: ModelSignal<string> = model<string>("");
    
    public readonly name: InputSignal<string> = input<string>("");
    public readonly label: InputSignal<string> = input<string>("");
    public readonly placeholder: InputSignal<string> = input<string>("");
    public readonly hint: InputSignal<string> = input<string>("");
    public readonly error: InputSignal<string> = input<string>("");
    public readonly icon: InputSignal<string> = input<string>("");

    public readonly type: InputSignal<InputType> = input<InputType>("text");
    public readonly size: InputSignal<InputSize> = input<InputSize>("large");
    public readonly theme: InputSignal<InputTheme> = input<InputTheme>("default");

    public readonly isDisabled: InputSignal<boolean> = input<boolean>(false);
    public readonly isReadonly: InputSignal<boolean> = input<boolean>(false);
    public readonly isRequired: InputSignal<boolean> = input<boolean>(false);
    public readonly isFullWidth: InputSignal<boolean> = input<boolean>(false);
    public readonly minNumber: InputSignal<number> = input<number>(0);
    public readonly maxNumber: InputSignal<number> = input<number>(Infinity);

    public readonly valueChange: OutputEmitterRef<string> = output<string>();
    public readonly inputFocus: OutputEmitterRef<FocusEvent> = output<FocusEvent>();
    public readonly inputBlur: OutputEmitterRef<FocusEvent> = output<FocusEvent>();

    public readonly nativeInputType = computed<InputInputType>(() => {
        switch (this.type()) {
            case "phone":
                return "tel";
            case "password":
                return "password";
            default:
                return "text";
        }
    });

    public readonly inputMode = computed<"text" | "numeric" | "tel">(() => {
        switch (this.type()) {
            case "number":
                return "numeric";
            case "phone":
                return "tel";
            default:
                return "text";
        }
    });

    public handleInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        let nextValue: string = target.value;

        if (this.type() === "number") {
            nextValue = nextValue.replace(/[^0-9]/g, "");
            target.value = nextValue;
        } else if (this.type() === "phone") {
            nextValue = nextValue.replace(/[^0-9]/g, "").slice(0, 10);
            target.value = nextValue;
        }

        this.valueChange.emit(nextValue);
    }

    public handleChange(event: Event): void {
        if (this.type() !== "number") {
            return;
        }

        const target = event.target as HTMLInputElement;
        const minValue: number = Math.min(this.minNumber(), this.maxNumber());
        const maxValue: number = Math.max(this.minNumber(), this.maxNumber());

        const numericValue: number = parseInt(target.value.replace(/[^0-9]/g, ""), 10);
        const nextNumber: number = Number.isNaN(numericValue)
            ? minValue
            : Math.min(Math.max(numericValue, minValue), maxValue);

        const nextValue: string = nextNumber.toString();
        target.value = nextValue;
        this.valueChange.emit(nextValue);
    }

    public handleFocus(event: FocusEvent): void {
        this.inputFocus.emit(event);
    }

    public handleBlur(event: FocusEvent): void {
        this.inputBlur.emit(event);
    }
}
