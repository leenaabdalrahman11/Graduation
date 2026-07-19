import React, { useMemo, useState } from "react";
import {
  AlertCircle,
  CheckCircle2,
  CirclePlus,
  Languages,
  LoaderCircle,
  Power,
  RefreshCw,
  Search,
  Tags,
  Trash2,
  X,
} from "lucide-react";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  adminApi,
  extractList,
} from "../../services/adminApi";
import "./AdminCategories.css";

function readValue(object, ...keys) {
  for (const key of keys) {
    const value = object?.[key];

    if (value !== undefined && value !== null) {
      return value;
    }
  }

  return null;
}

function getCategoryId(category) {
  return readValue(category, "id", "Id");
}

function getCategoryTranslations(category) {
  const translations = readValue(
    category,
    "translations",
    "Translations",
    "categoryTranslations",
    "CategoryTranslations"
  );

  return Array.isArray(translations) ? translations : [];
}

function getTranslationName(category, language) {
  const directName =
    language === "ar"
      ? readValue(
          category,
          "nameAr",
          "NameAr",
          "arabicName",
          "ArabicName"
        )
      : readValue(
          category,
          "nameEn",
          "NameEn",
          "englishName",
          "EnglishName"
        );

  if (directName) {
    return directName;
  }

  const translations = getCategoryTranslations(category);

  const translation = translations.find((item) => {
    const itemLanguage = readValue(
      item,
      "language",
      "Language"
    );

    return (
      String(itemLanguage || "").toLowerCase() ===
      language.toLowerCase()
    );
  });

  return (
    readValue(translation, "name", "Name") ||
    ""
  );
}

function getCategoryDisplayName(category) {
  return (
    getTranslationName(category, "en") ||
    getTranslationName(category, "ar") ||
    readValue(category, "name", "Name") ||
    `Category #${getCategoryId(category) ?? ""}`
  );
}

function getCategoryStatus(category) {
  return readValue(category, "status", "Status");
}

function isCategoryActive(category) {
  const status = getCategoryStatus(category);

  if (typeof status === "boolean") {
    return status;
  }

  const normalizedStatus = String(status ?? "")
    .trim()
    .toLowerCase();

  return (
    status === 1 ||
    normalizedStatus === "1" ||
    normalizedStatus === "active" ||
    normalizedStatus === "enabled"
  );
}

function buildCategoryPayload(form) {
  const nameAr = form.nameAr.trim();
  const nameEn = form.nameEn.trim();
  const status = Number(form.status);

  const translations = [
    {
      name: nameAr,
      language: "ar",
    },
    {
      name: nameEn,
      language: "en",
    },
  ];

  return {
    status,

    nameAr,
    nameEn,

    translations,
  };
}

const initialForm = {
  nameAr: "",
  nameEn: "",
  status: "1",
};

export default function AdminCategories() {
  const queryClient = useQueryClient();

  const [searchText, setSearchText] = useState("");
  const [showCreateModal, setShowCreateModal] =
    useState(false);

  const [form, setForm] = useState(initialForm);
  const [formError, setFormError] = useState("");

  const {
    data: categories = [],
    isLoading,
    isFetching,
    error,
    refetch,
  } = useQuery({
    queryKey: ["admin-categories"],

    queryFn: async () => {
      const response = await adminApi.getCategories();
      return extractList(response);
    },
  });

  const createMutation = useMutation({
    mutationFn: (payload) =>
      adminApi.createCategory(payload),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-categories"],
      });

      setForm(initialForm);
      setFormError("");
      setShowCreateModal(false);
    },
  });

  const toggleStatusMutation = useMutation({
    mutationFn: (categoryId) =>
      adminApi.toggleCategoryStatus(categoryId),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-categories"],
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (categoryId) =>
      adminApi.deleteCategory(categoryId),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-categories"],
      });
    },
  });

  const filteredCategories = useMemo(() => {
    const normalizedSearch = searchText
      .trim()
      .toLowerCase();

    if (!normalizedSearch) {
      return categories;
    }

    return categories.filter((category) => {
      const id = getCategoryId(category);
      const englishName =
        getTranslationName(category, "en");
      const arabicName =
        getTranslationName(category, "ar");
      const displayName =
        getCategoryDisplayName(category);

      const searchableText = [
        id,
        englishName,
        arabicName,
        displayName,
      ]
        .join(" ")
        .toLowerCase();

      return searchableText.includes(normalizedSearch);
    });
  }, [categories, searchText]);

  function handleFormChange(event) {
    const { name, value } = event.target;

    setForm((currentForm) => ({
      ...currentForm,
      [name]: value,
    }));

    setFormError("");
  }

  async function handleCreateCategory(event) {
    event.preventDefault();

    setFormError("");

    if (!form.nameAr.trim()) {
      setFormError("Please enter the Arabic category name.");
      return;
    }

    if (!form.nameEn.trim()) {
      setFormError("Please enter the English category name.");
      return;
    }

    const payload = buildCategoryPayload(form);

    try {
      await createMutation.mutateAsync(payload);
    } catch (mutationError) {
      setFormError(
        mutationError?.message ||
          "Failed to create the category."
      );
    }
  }

  async function handleToggleStatus(category) {
    const categoryId = getCategoryId(category);

    if (!categoryId) {
      return;
    }

    try {
      await toggleStatusMutation.mutateAsync(categoryId);
    } catch (mutationError) {
      window.alert(
        mutationError?.message ||
          "Failed to change category status."
      );
    }
  }

  async function handleDelete(category) {
    const categoryId = getCategoryId(category);
    const categoryName =
      getCategoryDisplayName(category);

    if (!categoryId) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete "${categoryName}"?`
    );

    if (!confirmed) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(categoryId);
    } catch (mutationError) {
      window.alert(
        mutationError?.message ||
          "Failed to delete the category."
      );
    }
  }

  function closeCreateModal() {
    if (createMutation.isPending) {
      return;
    }

    setShowCreateModal(false);
    setForm(initialForm);
    setFormError("");
  }

  return (
    <section>
      <div className="admin-page-heading admin-categories-heading">
        <div>
          <span>Store Management</span>

          <h2>Categories</h2>

          <p>
            Manage category names, languages, and status.
          </p>
        </div>

        <div className="admin-categories-heading-actions">
          <button
            type="button"
            className="admin-categories-refresh"
            onClick={() => refetch()}
            disabled={isFetching}
          >
            <RefreshCw
              size={18}
              className={isFetching ? "spinning" : ""}
            />

            {isFetching ? "Refreshing..." : "Refresh"}
          </button>

          <button
            type="button"
            className="admin-category-create-button"
            onClick={() => setShowCreateModal(true)}
          >
            <CirclePlus size={18} />
            Add Category
          </button>
        </div>
      </div>

      <div className="admin-categories-toolbar">
        <div className="admin-categories-search">
          <Search size={18} />

          <input
            type="search"
            value={searchText}
            onChange={(event) =>
              setSearchText(event.target.value)
            }
            placeholder="Search categories..."
          />
        </div>

        <div className="admin-categories-count">
          <Tags size={17} />

          <span>
            {filteredCategories.length} categor
            {filteredCategories.length === 1
              ? "y"
              : "ies"}
          </span>
        </div>
      </div>

      {isLoading && (
        <div className="admin-categories-state">
          <LoaderCircle
            className="spinning"
            size={38}
          />

          <h3>Loading categories...</h3>

          <p>
            Fetching category data from the API.
          </p>
        </div>
      )}

      {!isLoading && error && (
        <div className="admin-categories-state admin-categories-error">
          <AlertCircle size={40} />

          <h3>Failed to load categories</h3>

          <p>{error.message}</p>

          <button
            type="button"
            onClick={() => refetch()}
          >
            Try again
          </button>
        </div>
      )}

      {!isLoading &&
        !error &&
        filteredCategories.length === 0 && (
          <div className="admin-categories-state">
            <Tags size={42} />

            <h3>No categories found</h3>

            <p>
              {searchText
                ? "No categories match your search."
                : "Create your first category using the Add Category button."}
            </p>
          </div>
        )}

      {!isLoading &&
        !error &&
        filteredCategories.length > 0 && (
          <div className="admin-categories-table-wrapper">
            <table className="admin-categories-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>English Name</th>
                  <th>Arabic Name</th>
                  <th>Status</th>
                  <th className="admin-category-actions-heading">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody>
                {filteredCategories.map(
                  (category, index) => {
                    const categoryId =
                      getCategoryId(category);

                    const englishName =
                      getTranslationName(
                        category,
                        "en"
                      );

                    const arabicName =
                      getTranslationName(
                        category,
                        "ar"
                      );

                    const active =
                      isCategoryActive(category);

                    return (
                      <tr key={categoryId ?? index}>
                        <td>#{categoryId ?? "—"}</td>

                        <td>
                          <div className="admin-category-name">
                            <div className="admin-category-icon">
                              <Tags size={18} />
                            </div>

                            <div>
                              <strong>
                                {englishName ||
                                  getCategoryDisplayName(
                                    category
                                  )}
                              </strong>

                              <span>English</span>
                            </div>
                          </div>
                        </td>

                        <td>
                          <div className="admin-category-language">
                            <Languages size={16} />

                            <span dir="rtl">
                              {arabicName || "—"}
                            </span>
                          </div>
                        </td>

                        <td>
                          <span
                            className={
                              active
                                ? "admin-category-status active"
                                : "admin-category-status inactive"
                            }
                          >
                            {active ? (
                              <CheckCircle2 size={14} />
                            ) : (
                              <Power size={14} />
                            )}

                            {active
                              ? "Active"
                              : "Inactive"}
                          </span>
                        </td>

                        <td>
                          <div className="admin-category-actions">
                            <button
                              type="button"
                              className="admin-category-toggle"
                              onClick={() =>
                                handleToggleStatus(
                                  category
                                )
                              }
                              disabled={
                                toggleStatusMutation.isPending
                              }
                              title="Change category status"
                            >
                              <Power size={17} />
                            </button>

                            <button
                              type="button"
                              className="admin-category-delete"
                              onClick={() =>
                                handleDelete(category)
                              }
                              disabled={
                                deleteMutation.isPending
                              }
                              title="Delete category"
                            >
                              <Trash2 size={17} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  }
                )}
              </tbody>
            </table>
          </div>
        )}

      {showCreateModal && (
        <div
          className="admin-category-modal-overlay"
          role="presentation"
        >
          <div
            className="admin-category-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-category-title"
          >
            <div className="admin-category-modal-header">
              <div>
                <span>New Category</span>

                <h3 id="create-category-title">
                  Create Category
                </h3>

                <p>
                  Add the Arabic and English category
                  names.
                </p>
              </div>

              <button
                type="button"
                onClick={closeCreateModal}
                disabled={createMutation.isPending}
                aria-label="Close"
              >
                <X size={21} />
              </button>
            </div>

            <form
              onSubmit={handleCreateCategory}
              className="admin-category-form"
            >
              <div className="admin-category-field">
                <label htmlFor="category-name-en">
                  English Name
                </label>

                <input
                  id="category-name-en"
                  type="text"
                  name="nameEn"
                  value={form.nameEn}
                  onChange={handleFormChange}
                  placeholder="Example: Bracelets"
                  autoComplete="off"
                />
              </div>

              <div className="admin-category-field">
                <label htmlFor="category-name-ar">
                  Arabic Name
                </label>

                <input
                  id="category-name-ar"
                  type="text"
                  name="nameAr"
                  value={form.nameAr}
                  onChange={handleFormChange}
                  placeholder="مثال: أساور"
                  autoComplete="off"
                  dir="rtl"
                />
              </div>

              <div className="admin-category-field">
                <label htmlFor="category-status">
                  Status
                </label>

                <select
                  id="category-status"
                  name="status"
                  value={form.status}
                  onChange={handleFormChange}
                >
                  <option value="1">Active</option>
                  <option value="0">Inactive</option>
                </select>
              </div>

              {formError && (
                <div className="admin-category-form-error">
                  <AlertCircle size={17} />
                  <span>{formError}</span>
                </div>
              )}

              <div className="admin-category-form-actions">
                <button
                  type="button"
                  className="admin-category-cancel"
                  onClick={closeCreateModal}
                  disabled={createMutation.isPending}
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  className="admin-category-submit"
                  disabled={createMutation.isPending}
                >
                  {createMutation.isPending ? (
                    <>
                      <LoaderCircle
                        size={18}
                        className="spinning"
                      />
                      Creating...
                    </>
                  ) : (
                    <>
                      <CirclePlus size={18} />
                      Create Category
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  );
}