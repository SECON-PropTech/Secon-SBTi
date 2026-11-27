import { Plus } from "lucide-react";


type Props = {
    text: string;
    onClick: () => void;
    warning?: boolean;
};

//a dashed "+ Lägg till" button for a value that is missing
function AddButton({ text, onClick, warning = false}: Props) {
    return (
        <button
            type="button"
            className={warning ? "energy-add-button energy-add-button-warning" : "energy-add-button"}
            onClick={(event) => {
                event.stopPropagation();
                onClick();
            }}>
            <Plus size={14}/> {text}
        </button>
    );
}

export default AddButton;
