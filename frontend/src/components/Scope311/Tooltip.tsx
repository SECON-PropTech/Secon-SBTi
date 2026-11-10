import type { ReactNode } from "react";

type Props = {
    text: string;
    children: ReactNode;
};

//shows a small dark box with an explanation when the mouse is over the children
//it's only CSS (see .energy-tooltip in energyConsumption.css)
function Tooltip({ text, children }: Props) {
    return (
        <span className="energy-tooltip" data-tooltip={text}>
            {children}
        </span>
    );
}

export default Tooltip;