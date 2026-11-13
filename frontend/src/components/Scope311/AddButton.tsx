import { Plus } from "lucide-react";


type Props = {
    text: string;
    warning?: boolean;
};

//a dashed "+ Lägg till" button for a value that is missing
//disabled until editing is built
function AddButton({ text, warning = false}: Props) {
    return (
        <button
            type="button"
            className={warning ? "energy-add-button energy-add-button-warning" : "energy-add-button"}
            disabled>
            <Plus size={14}/> {text}
            </button>
    );
}

export default AddButton;
