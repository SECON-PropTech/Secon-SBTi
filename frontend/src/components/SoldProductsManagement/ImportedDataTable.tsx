import type { Product, ProductInclusion } from "../../types/soldProductsManagement";

type Props = {
    products: Product[];
    productInclusion: ProductInclusion;
    onToggleInclusion: (productId: string) => void;
    onEditProduct?: (productId: string, updatedProduct: Product) => void;
};

function ImportedDataTable({
    products,
    productInclusion,
    onToggleInclusion,
}: Props) {
    return (
        <div className="table-responsive">
            <table className="table">
                <thead>
                    <tr>
                        <th>Inkludera</th>
                        <th>Produkt</th>
                        <th>Antal</th>
                        <th>Material</th>
                        <th>Datum</th>
                    </tr>
                </thead>
                <tbody>
                    {products.map((product) => (
                        <tr key={product.id}>
                            <td>
                                <input
                                    type="checkbox"
                                    checked={productInclusion[product.id] === true}
                                    onChange={() => onToggleInclusion(product.id)}
                                />
                            </td>
                            <td>{product.name}</td>
                            <td>{product.quantity}</td>
                            <td>
                                {product.materials.map((material, index) => (
                                <div key={index}>
                                    {material.name} ({material.kg} kg)
                                </div>
                                ))}
                            </td>
                            <td>{product.date}</td>                        
                        </tr>
                    ))}
                </tbody>
            </table>

        </div>
    )
}

export default ImportedDataTable