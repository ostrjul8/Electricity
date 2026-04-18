import { Component, computed, input, InputSignal, model, ModelSignal, output, OutputEmitterRef } from "@angular/core";

type RangeSize = "small" | "large";
type RangeTheme = "default" | "ghost";

@Component({
    selector: "ui-range",
    imports: [],
    templateUrl: "./range.html",
    styleUrl: "./range.css",
})
export class Range {
    public readonly name: InputSignal<string> = input<string>("");
    public readonly label: InputSignal<string> = input<string>("");
    public readonly hint: InputSignal<string> = input<string>("");
    public readonly error: InputSignal<string> = input<string>("");

    public readonly value: ModelSignal<number> = model.required<number>();
    public readonly minNumber: InputSignal<number> = input<number>(0);
    public readonly maxNumber: InputSignal<number> = input<number>(100);
    public readonly step: InputSignal<number> = input<number>(1);

    public readonly size: InputSignal<RangeSize> = input<RangeSize>("large");
    public readonly theme: InputSignal<RangeTheme> = input<RangeTheme>("default");

    public readonly isDisabled: InputSignal<boolean> = input<boolean>(false);
    public readonly isRequired: InputSignal<boolean> = input<boolean>(false);
    public readonly isFullWidth: InputSignal<boolean> = input<boolean>(false);
    public readonly showValue: InputSignal<boolean> = input<boolean>(true);
    public readonly showBoundaries: InputSignal<boolean> = input<boolean>(true);

    public readonly valueChange: OutputEmitterRef<number> = output<number>();
    public readonly rangeFocus: OutputEmitterRef<FocusEvent> = output<FocusEvent>();
    public readonly rangeBlur: OutputEmitterRef<FocusEvent> = output<FocusEvent>();

    public readonly normalizedMin = computed<number>(() => Math.min(this.minNumber(), this.maxNumber()));

    public readonly normalizedMax = computed<number>(() => Math.max(this.minNumber(), this.maxNumber()));

    public readonly normalizedStep = computed<number>(() => (this.step() > 0 ? this.step() : 1));

    public readonly clampedValue = computed<number>(() => {
        const value: number = Number.isFinite(this.value()) ? this.value() : this.normalizedMin();

        return Math.min(Math.max(value, this.normalizedMin()), this.normalizedMax());
    });

    public readonly progressPercent = computed<number>(() => {
        const minValue: number = this.normalizedMin();
        const maxValue: number = this.normalizedMax();

        if (maxValue <= minValue) {
            return 0;
        }

        return ((this.clampedValue() - minValue) / (maxValue - minValue)) * 100;
    });

    public handleInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        const rawValue: number = Number.parseFloat(target.value);

        if (Number.isNaN(rawValue)) {
            return;
        }

        const nextValue: number = Math.min(Math.max(rawValue, this.normalizedMin()), this.normalizedMax());

        target.value = nextValue.toString();
        this.valueChange.emit(nextValue);
    }

    public handleChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        const rawValue: number = Number.parseFloat(target.value);

        const nextValue: number = Number.isNaN(rawValue)
            ? this.normalizedMin()
            : Math.min(Math.max(rawValue, this.normalizedMin()), this.normalizedMax());

        target.value = nextValue.toString();
        this.valueChange.emit(nextValue);
    }

    public handleFocus(event: FocusEvent): void {
        this.rangeFocus.emit(event);
    }

    public handleBlur(event: FocusEvent): void {
        this.rangeBlur.emit(event);
    }
}
