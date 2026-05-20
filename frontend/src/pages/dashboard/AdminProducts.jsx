import React, { useMemo, useState } from "react";
import {
  AlertCircle,
  ImageOff,
  Package,
  RefreshCw,
  Search,
  Plus,
  Trash2,
} from "lucide-react";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { adminApi, extractList } from "../../services/adminApi";
import "./AdminProducts.css";

const baseUrl = (import.meta.env.VITE_API_URL || "").replace(/\/$/, "");

function readValue(object, ...keys) {
  for (const key of keys) {
    const value = object?.[key];

    if (value !== undefined && value !== null) {
      return value;
    }
  }

  return null;
}

function getTranslations(product) {
  return (
    readValue(product, "translations", "Translations") || []
  );
}

function getProductName(product) {
  const directName = readValue(product, "name", "Name");

  if (directName) {
    return directName;
  }

  const translations = getTranslations(product);

  const englishTranslation =
    translations.find((translation) => {
      const language = readValue(
        translation,
        "language",
        "Language"
      );

      return String(language).toLowerCase() === "en";
    }) || translations[0];

  return (
    readValue(englishTranslation, "name", "Name") ||
    `Product #${readValue(product, "id", "Id") ?? ""}`
  );
}

function getProductDescription(product) {
  const directDescription = readValue(
    product,
    "description",
    "Description"
  );

  if (directDescription) {
    return directDescription;
  }

  const translation = getTranslations(product)[0];

  return (
    readValue(translation, "description", "Description") || ""
  );
}

function getImageUrl(product) {
  const image = readValue(
    product,
    "mainImage",
    "MainImage",
    "image",
    "Image"
  );

  if (!image) {
    return null;
  }

  if (
    image.startsWith("http://") ||
    image.startsWith("https://") ||
    image.startsWith("data:")
  ) {
    return image;
  }

  return `${baseUrl}/${image.replace(/^\/+/, "")}`;
}

function formatPrice(value) {
  const number = Number(value);

  if (Number.isNaN(number)) {
    return value ?? "—";
  }

  return `$${number.toFixed(2)}`;
}
const emptyProductForm = {
  englishName: "",
  englishDescription: "",
  arabicName: "",
  arabicDescription: "",
  price: "",
  quantity: "",
  categoryId: "",
  discount: "0",
  mainImage: null,
};
export default function AdminProducts() {
  const queryClient = useQueryClient();

  const [searchText, setSearchText] = useState("");
const [showAddForm, setShowAddForm] = useState(false);

const [productForm, setProductForm] = useState(
  emptyProductForm
);

const [createError, setCreateError] = useState("");
  const {
    data: products = [],
    isLoading,
    isFetching,
    error,
    refetch,
  } = useQuery({
    queryKey: ["admin-products"],

    queryFn: async () => {
      const result = await adminApi.getProducts();
      return extractList(result);
    },
  });
const createMutation = useMutation({
  mutationFn: (formData) =>
    adminApi.createProduct(formData),

  onSuccess: async () => {
    setProductForm(emptyProductForm);
    setCreateError("");
    setShowAddForm(false);

    await queryClient.invalidateQueries({
      queryKey: ["admin-products"],
    });
  },

  onError: (mutationError) => {
    setCreateError(
      mutationError?.message ||
        "Failed to create product."
    );
  },
});
  const deleteMutation = useMutation({
    mutationFn: (productId) =>
      adminApi.deleteProduct(productId),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-products"],
      });
    },
  });

  const filteredProducts = useMemo(() => {
    const normalizedSearch = searchText
      .trim()
      .toLowerCase();

    if (!normalizedSearch) {
      return products;
    }

    return products.filter((product) => {
      const id = readValue(product, "id", "Id");
      const name = getProductName(product);
      const description = getProductDescription(product);

      const searchableText =
        `${id ?? ""} ${name} ${description}`.toLowerCase();

      return searchableText.includes(normalizedSearch);
    });
  }, [products, searchText]);
function handleProductChange(event) {
  const { name, value, files } = event.target;

  setProductForm((currentForm) => ({
    ...currentForm,
    [name]: files ? files[0] : value,
  }));
}

async function handleCreateProduct(event) {
  event.preventDefault();
  setCreateError("");

  if (!productForm.englishName.trim()) {
    setCreateError("English product name is required.");
    return;
  }

  if (!productForm.price) {
    setCreateError("Price is required.");
    return;
  }

  if (!productForm.quantity) {
    setCreateError("Quantity is required.");
    return;
  }

  if (!productForm.categoryId) {
    setCreateError("Category ID is required.");
    return;
  }

  if (!productForm.mainImage) {
    setCreateError("Main image is required.");
    return;
  }

const formData = new FormData();

formData.append("Price", productForm.price);

formData.append(
  "Stock",
  Number(productForm.quantity) > 0 ? "1" : "0"
);

formData.append("Quantity", productForm.quantity);
formData.append("CategoryId", productForm.categoryId);
formData.append("Discount", productForm.discount);

if (productForm.mainImage) {
  formData.append("MainImage", productForm.mainImage);
}

formData.append(
  "Translations[0].Language",
  "en"
);

formData.append(
  "Translations[0].Name",
  productForm.englishName
);

formData.append(
  "Translations[0].Description",
  productForm.englishDescription || ""
);

if (productForm.arabicName.trim()) {
  formData.append(
    "Translations[1].Language",
    "ar"
  );

  formData.append(
    "Translations[1].Name",
    productForm.arabicName
  );

  formData.append(
    "Translations[1].Description",
    productForm.arabicDescription || ""
  );
}

await createMutation.mutateAsync(formData);
}
  async function handleDelete(product) {
    const id = readValue(product, "id", "Id");
    const name = getProductName(product);

    if (!id) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete "${name}"?`
    );

    if (!confirmed) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(id);
    } catch (mutationError) {
      window.alert(
        mutationError?.message || "Failed to delete product."
      );
    }
  }

  return (
    <section>
      <div className="admin-page-heading admin-products-heading">
        <div>
          <span>Store Management</span>
          <h2>Products</h2>
          <p>
            View and manage the products stored in your database.
          </p>
        </div>
<div className="admin-products-heading-actions">
    <button
  type="button"
  className="admin-product-add-button"
  onClick={() => {
    setShowAddForm((currentValue) => !currentValue);
    setCreateError("");
  }}
>
  <Plus size={18} />

  {showAddForm ? "Close Form" : "Add Product"}
</button>
        <button
          type="button"
          className="admin-products-refresh"
          onClick={() => refetch()}
          disabled={isFetching}
        >
          <RefreshCw
            size={18}
            className={isFetching ? "spinning" : ""}
          />

          {isFetching ? "Refreshing..." : "Refresh"}
        </button>
</div>

      </div>
{showAddForm && (
  <form
    className="admin-product-create-form"
    onSubmit={handleCreateProduct}
  >
    <div className="admin-product-create-header">
      <div>
        <h3>Add New Product</h3>
        <p>Enter the new product information.</p>
      </div>
    </div>

    {createError && (
      <div className="admin-product-create-error">
        <AlertCircle size={18} />
        <span>{createError}</span>
      </div>
    )}

    <div className="admin-product-create-grid">
      <label>
        English Name
        <input
          type="text"
          name="englishName"
          value={productForm.englishName}
          onChange={handleProductChange}
          required
        />
      </label>

      <label>
        Arabic Name
        <input
          type="text"
          name="arabicName"
          value={productForm.arabicName}
          onChange={handleProductChange}
        />
      </label>

      <label>
        Price
        <input
          type="number"
          name="price"
          min="0"
          step="0.01"
          value={productForm.price}
          onChange={handleProductChange}
          required
        />
      </label>

      <label>
        Quantity
        <input
          type="number"
          name="quantity"
          min="0"
          value={productForm.quantity}
          onChange={handleProductChange}
          required
        />
      </label>

      <label>
        Category ID
        <input
          type="number"
          name="categoryId"
          min="1"
          value={productForm.categoryId}
          onChange={handleProductChange}
          required
        />
      </label>

      <label>
        Discount
        <input
          type="number"
          name="discount"
          min="0"
          step="0.01"
          value={productForm.discount}
          onChange={handleProductChange}
        />
      </label>

      <label className="admin-product-full-field">
        English Description
        <textarea
          name="englishDescription"
          value={productForm.englishDescription}
          onChange={handleProductChange}
          rows={3}
        />
      </label>

      <label className="admin-product-full-field">
        Arabic Description
        <textarea
          name="arabicDescription"
          value={productForm.arabicDescription}
          onChange={handleProductChange}
          rows={3}
        />
      </label>

      <label className="admin-product-full-field">
        Main Image
        <input
          type="file"
          name="mainImage"
          accept="image/*"
          onChange={handleProductChange}
          required
        />
      </label>
    </div>

    <div className="admin-product-create-actions">
      <button
        type="button"
        onClick={() => {
          setProductForm(emptyProductForm);
          setCreateError("");
          setShowAddForm(false);
        }}
      >
        Cancel
      </button>

      <button
        type="submit"
        disabled={createMutation.isPending}
      >
        {createMutation.isPending
          ? "Adding Product..."
          : "Add Product"}
      </button>
    </div>
  </form>
)}
      <div className="admin-products-toolbar">
        <div className="admin-products-search">
          <Search size={18} />

          <input
            type="search"
            value={searchText}
            onChange={(event) =>
              setSearchText(event.target.value)
            }
            placeholder="Search by product name or ID..."
          />
        </div>

        <div className="admin-products-total">
          <Package size={17} />

          <span>
            {filteredProducts.length} product
            {filteredProducts.length === 1 ? "" : "s"}
          </span>
        </div>
      </div>

      {isLoading && (
        <div className="admin-products-state">
          <RefreshCw className="spinning" size={35} />
          <h3>Loading products...</h3>
          <p>Fetching product data from the API.</p>
        </div>
      )}

      {!isLoading && error && (
        <div className="admin-products-state admin-products-error">
          <AlertCircle size={38} />
          <h3>Failed to load products</h3>
          <p>{error.message}</p>

          <button type="button" onClick={() => refetch()}>
            Try again
          </button>
        </div>
      )}

      {!isLoading &&
        !error &&
        filteredProducts.length === 0 && (
          <div className="admin-products-state">
            <Package size={40} />
            <h3>No products found</h3>

            <p>
              {searchText
                ? "No products match your search."
                : "The API returned an empty product list."}
            </p>
          </div>
        )}

      {!isLoading &&
        !error &&
        filteredProducts.length > 0 && (
          <div className="admin-products-table-wrapper">
            <table className="admin-products-table">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>ID</th>
                  <th>Price</th>
                  <th>Quantity</th>
                  <th>Stock</th>
                  <th>Status</th>
                  <th className="admin-products-actions-heading">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody>
                {filteredProducts.map((product, index) => {
                  const id = readValue(product, "id", "Id");
                  const price = readValue(
                    product,
                    "price",
                    "Price"
                  );

                  const quantity = readValue(
                    product,
                    "quantity",
                    "Quantity"
                  );

                  const stock = readValue(
                    product,
                    "stock",
                    "Stock"
                  );

                  const status = readValue(
                    product,
                    "status",
                    "Status"
                  );

                  const imageUrl = getImageUrl(product);

                  return (
                    <tr key={id ?? index}>
                      <td>
                        <div className="admin-product-information">
                          <div className="admin-product-image">
                            {imageUrl ? (
                              <img
                                src={`${imageUrl}`}
                                alt={getProductName(product)}
                                onError={(event) => {
                                  event.currentTarget.style.display =
                                    "none";
                                }}
                              />
                            ) : (
                              <ImageOff size={20} />
                            )}
                          </div>

                          <div>
                            <strong>
                              {getProductName(product)}
                            </strong>

                            <p>
                              {getProductDescription(product) ||
                                "No description available"}
                            </p>
                          </div>
                        </div>
                      </td>

                      <td>#{id ?? "—"}</td>

                      <td>
                        <strong>{formatPrice(price)}</strong>
                      </td>

                      <td>{quantity ?? "—"}</td>

                      <td>{stock ?? "—"}</td>

                      <td>
                        <span className="admin-product-status">
                          {status ?? "—"}
                        </span>
                      </td>

                      <td>
                        <div className="admin-product-actions">
                          <button
                            type="button"
                            className="admin-product-delete"
                            onClick={() =>
                              handleDelete(product)
                            }
                            disabled={
                              deleteMutation.isPending
                            }
                            title="Delete product"
                          >
                            <Trash2 size={17} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
    </section>
  );
}