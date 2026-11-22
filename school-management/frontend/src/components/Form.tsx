import { useState } from "react";
import { Row, kes } from "../api";
import { schemas, Field } from "../schema";
export function Form({
  resource,
  initial,
  lookups,
  admin,
  departmentId,
  onSave,
  onCancel,
}: {
  resource: string;
  initial: Row;
  lookups: Record<string, Row[]>;
  admin: boolean;
  departmentId?: number;
  onSave: (v: Row) => Promise<void>;
  onCancel: () => void;
}) {
  const schema = schemas[resource];
  const [data, setData] = useState<Row>(() =>
    Object.fromEntries(
      schema.fields.map((f) => [
        f.key,
        initial[f.key] ??
          (f.key === "departmentId" && !admin
            ? departmentId
            : f.type === "checkbox"
              ? f.key === "active"
              : f.type === "date"
                ? new Date().toISOString().slice(0, 10)
                : f.type === "select" && f.options
                  ? f.options[0]
                  : f.key === "year"
                    ? "2026"
                    : ""),
      ]),
    ),
  );
  const [items, setItems] = useState<Row[]>(
    initial.items || [{ description: "", quantity: 1, unitPrice: "" }],
  );
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  function change(f: Field, v: any) {
    setData({
      ...data,
      [f.key]:
        f.type === "number" || (f.type === "select" && f.source)
          ? v === ""
            ? null
            : Number(v)
          : v,
    });
  }
  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const result = { ...data };
      for (const f of schema.fields)
        if (f.source && result[f.key] === "") result[f.key] = null;
      for (const f of schema.fields)
        if (
          f.type === "number" &&
          (result[f.key] === "" || result[f.key] == null)
        )
          result[f.key] = 0;
      await onSave({
        ...result,
        ...(resource === "requests" ? { items } : {}),
      });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form onSubmit={submit}>
      <div className="form-grid">
        {schema.fields
          .filter((f) => admin || f.key !== "overrideReason")
          .map((f) => (
            <label key={f.key} className={f.type === "textarea" ? "wide" : ""}>
              {f.label}
              {!f.optional && f.type !== "checkbox" && (
                <span className="required"> *</span>
              )}
              {f.type === "select" ? (
                <select
                  required={!f.optional}
                  disabled={!admin && f.key === "departmentId"}
                  value={data[f.key] ?? ""}
                  onChange={(e) => change(f, e.target.value)}
                >
                  <option value="">Select…</option>
                  {f.options
                    ?.filter(
                      (o) =>
                        admin ||
                        resource !== "petty-transactions" ||
                        !["Opening", "Replenishment"].includes(o),
                    )
                    .map((o) => (
                      <option key={o}>{o}</option>
                    ))}
                  {f.source &&
                    lookups[f.source]
                      ?.filter(
                        (r) =>
                          r.active !== false &&
                          (f.key !== "withdrawalId" ||
                            (r.kind === "Withdrawal" &&
                              r.accountId === data.accountId)) &&
                          (f.key !== "budgetId" ||
                            !data.departmentId ||
                            r.departmentId === data.departmentId) &&
                          (f.key !== "invoiceId" ||
                            !data.purchaseRequestId ||
                            r.purchaseRequestId === data.purchaseRequestId),
                      )
                      .map((r) => (
                        <option key={r.id} value={r.id}>
                          {r.name || r.purpose || r.number || "#" + r.id}
                          {r.remaining !== undefined
                            ? " · KES " + r.remaining.toLocaleString()
                            : ""}
                        </option>
                      ))}
                </select>
              ) : f.type === "textarea" ? (
                <textarea
                  maxLength={4000}
                  required={!f.optional}
                  value={data[f.key] || ""}
                  onChange={(e) => change(f, e.target.value)}
                />
              ) : (
                <input
                  type={f.type || "text"}
                  maxLength={f.type === "number" ? undefined : 4000}
                  step={f.type === "number" ? ".01" : undefined}
                  required={!f.optional && f.type !== "checkbox"}
                  checked={f.type === "checkbox" ? !!data[f.key] : undefined}
                  value={
                    f.type === "checkbox" ? undefined : (data[f.key] ?? "")
                  }
                  onChange={(e) =>
                    change(
                      f,
                      f.type === "checkbox" ? e.target.checked : e.target.value,
                    )
                  }
                />
              )}
            </label>
          ))}
      </div>
      {resource === "requests" && (
        <div className="line-items">
          <h3>Requested items</h3>
          <div className="request-estimate">
            <strong>
              Estimated total:{" "}
              {kes(
                items.reduce(
                  (sum, item) =>
                    sum +
                    Number(item.quantity || 0) * Number(item.unitPrice || 0),
                  0,
                ),
              )}
            </strong>
            {data.budgetId && (
              <span>
                Available:{" "}
                {kes(
                  lookups.budgets?.find((b) => b.id === data.budgetId)
                    ?.remaining,
                )}
              </span>
            )}
          </div>
          {data.budgetId &&
            items.reduce(
              (sum, item) =>
                sum + Number(item.quantity || 0) * Number(item.unitPrice || 0),
              0,
            ) >
              (lookups.budgets?.find((b) => b.id === data.budgetId)
                ?.remaining ?? 0) && (
              <p className="attention warning">
                This request exceeds the available budget. Admin approval with
                an override reason is required.
              </p>
            )}
          {items.map((item, i) => (
            <div className="item-row" key={i}>
              <input
                aria-label="Item description"
                placeholder="Item description"
                required
                value={item.description}
                onChange={(e) =>
                  setItems(
                    items.map((x, j) =>
                      j === i ? { ...x, description: e.target.value } : x,
                    ),
                  )
                }
              />
              <input
                aria-label="Quantity"
                type="number"
                min="0.01"
                step=".01"
                required
                value={item.quantity}
                onChange={(e) =>
                  setItems(
                    items.map((x, j) =>
                      j === i ? { ...x, quantity: Number(e.target.value) } : x,
                    ),
                  )
                }
              />
              <input
                aria-label="Unit price in KES"
                placeholder="Unit price (KES)"
                type="number"
                min=".01"
                step=".01"
                required
                value={item.unitPrice}
                onChange={(e) =>
                  setItems(
                    items.map((x, j) =>
                      j === i ? { ...x, unitPrice: Number(e.target.value) } : x,
                    ),
                  )
                }
              />
              <button
                type="button"
                disabled={items.length === 1}
                onClick={() => setItems(items.filter((_, j) => j !== i))}
              >
                Remove
              </button>
            </div>
          ))}
          <button
            type="button"
            onClick={() =>
              setItems([
                ...items,
                { description: "", quantity: 1, unitPrice: "" },
              ])
            }
          >
            + Add item
          </button>
        </div>
      )}
      {initial.active === true && data.active === false && (
        <label className="deactivate-confirm">
          <input type="checkbox" required /> Confirm deactivation. Existing
          records and audit history will be retained.
        </label>
      )}
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <div className="form-actions">
        <button type="button" onClick={onCancel}>
          Cancel
        </button>
        <button className="primary" disabled={busy}>
          {busy ? "Saving…" : "Save record"}
        </button>
      </div>
    </form>
  );
}
