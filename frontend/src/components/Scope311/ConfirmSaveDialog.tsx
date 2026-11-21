type Props = {
    productName: string;
    canSave: boolean;
    onKeepEditing: () => void;
    onDiscard: () => void;
    onSave: () => void;
};

// Asks what to do when a user clicks another pencil
// while you have changes that arent saved.
function ConfirmSaveDialog({ productName, canSave, onKeepEditing, onDiscard, onSave } : Props) {
    return (
        <>
            <div className ="modal d-block" role="dialog">
                <div className = "modal-dialog modal-dialog-centered">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title fs-5"> Spara ändringar?</h5>

                        </div>
                        <div className="modal-body">
                            <p className= "mb-0">
                                Du har ändringar för {productName} som 
                                inte är sparade.
                            </p>
                        </div>
                        <div className = "modal-footer">
                            <button type="button" className="btn btn-outline-secondary"
                            onClick={onKeepEditing}>Fortsätt redigera
                            </button>
                            
                            <button type="button" className="btn btn-outline-secondary" onClick={onDiscard}>
                                Kasta ändringar
                            </button>
                            <button type="button" className="btn energy-save-button" onClick={onSave}
                            disabled={!canSave}>
                                Spara
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            <div className="modal-backdrop show"></div>
        </>
    );
}

export default ConfirmSaveDialog;