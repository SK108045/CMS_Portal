import { useEffect, useState } from "react";
import { api, Row, kes } from "../api";
import { schemas } from "../schema";
import { Table, label, value } from "../components/Table";
import { Form } from "../components/Form";
import { Modal } from "../components/Modal";
export function Records({
  resource,
  lookups,
  user,
  refresh,
  notify,
  approvalQueue = false,
}: {
  approvalQueue?: boolean;
  resource: string;
  lookups: Record<string, Row[]>;
  user: Row;
  refresh: () => Promise<void>;
  notify: (s: string) => void;
}) {
  const [rows, setRows] = useState<Row[]>([]);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(true);
  const [editing, setEditing] = useState<Row | null>(null);
  const [detail, setDetail] = useState<Row | null>(null);
  const [decision, setDecision] = useState("");
  const [comment, setComment] = useState("");
  const [amount, setAmount] = useState("");
  const [supplier, setSupplier] = useState("");
  const [working, setWorking] = useState(false);
  const [related, setRelated] = useState<Record<string, Row[]>>({});
  const [convert, setConvert] = useState(false);
  const [docs, setDocs] = useState<Row[]>([]);
  const [history, setHistory] = useState<Row[]>([]);
  const [file, setFile] = useState<File>();
  const [kind, setKind] = useState("Receipt");
  const admin = user.role === "ADMIN";
  const schema = schemas[resource];
  async function load() {
    setBusy(true);
    try {
      setRows(await api("records/" + resource));
      setError("");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    load();
    setDetail(null);
    setEditing(null);
  }, [resource]);
  useEffect(() => {
    if (detail) {
      const sources =
        resource === "requests"
          ? ["invoices", "payments", "approvals"]
          : resource === "suppliers"
            ? ["requests", "invoices", "payments"]
            : resource === "assets"
              ? ["expenses"]
              : resource === "inventory"
                ? ["movements"]
                : [];
      Promise.all(
        sources.map(
          async (s) =>
            [
              s,
              (await api("records/" + s)).filter((r: Row) =>
                resource === "requests"
                  ? r.purchaseRequestId === detail.id
                  : resource === "suppliers"
                    ? r.supplierId === detail.id ||
                      (r.purchaseRequestId &&
                        lookups.requests?.find(
                          (q) => q.id === r.purchaseRequestId,
                        )?.supplierId === detail.id)
                    : resource === "assets"
                      ? r.assetId === detail.id
                      : r.inventoryItemId === detail.id,
              ),
            ] as const,
        ),
      )
        .then((entries) => setRelated(Object.fromEntries(entries)))
        .catch((e) => setError(e.message));
      api("records/documents").then((r: Row[]) =>
        setDocs(
          r.filter((d) => d.entity === resource && d.entityId === detail.id),
        ),
      );
      api("records/audit").then((r: Row[]) =>
        setHistory(
          r.filter((d) => d.entity === resource && d.entityId === detail.id),
        ),
      );
      setAmount(String(detail.estimatedAmount || ""));
      setSupplier(String(detail.supplierId || ""));
      setComment("");
      setDecision("");
    }
  }, [detail, resource]);
  async function action(fn: () => Promise<any>) {
    setWorking(true);
    try {
      await fn();
      notify("Saved successfully");
      await load();
      await refresh();
      setDetail(null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setWorking(false);
    }
  }
  const canCreate = !schema.readOnly && (admin || schema.userCreate);
  const transitions = detail
    ? (
        {
          Draft: ["Submitted", "Cancelled"],
          "Returned for Changes": ["Submitted", "Cancelled"],
          Submitted: [
            "Under Review",
            "Approved",
            "Rejected",
            "Returned for Changes",
            "Cancelled",
          ],
          "Under Review": [
            "Approved",
            "Rejected",
            "Returned for Changes",
            "Cancelled",
          ],
          Approved: ["Ordered", "Partially Received", "Received", "Cancelled"],
          Ordered: ["Partially Received", "Received", "Cancelled"],
          "Partially Received": ["Received"],
          Paid: ["Completed"],
        } as Record<string, string[]>
      )[detail.status] || []
    : [];
  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">SCHOOL FINANCE</span>
          <h1>{approvalQueue ? "Pending Approvals" : schema.title}</h1>
          <p>{schema.description}</p>
        </div>
        <div className="actions">
          {resource === "budgets" && admin && (
            <button onClick={() => (location.hash = "adjustments")}>
              Adjustment history
            </button>
          )}
          {resource === "petty-accounts" && (
            <button onClick={() => (location.hash = "petty-requests")}>
              Request funds / decisions
            </button>
          )}
          {resource === "petty-accounts" && (
            <button onClick={() => (location.hash = "petty-transactions")}>
              View movements
            </button>
          )}
          {resource === "inventory" && (
            <button onClick={() => (location.hash = "movements")}>
              Stock movements
            </button>
          )}
          {resource === "payments" && admin && (
            <button onClick={() => (location.hash = "invoices")}>
              Invoices
            </button>
          )}
          {canCreate && (
            <button className="primary" onClick={() => setEditing({})}>
              + New {resource === "requests" ? "request" : "record"}
            </button>
          )}
        </div>
      </div>
      {error && (
        <div role="alert" className="error">
          {error}
          <button onClick={() => setError("")}>Dismiss</button>
        </div>
      )}
      {busy ? (
        <div className="loading">Loading records…</div>
      ) : (
        <Table
          rows={
            approvalQueue
              ? rows.filter((r) =>
                  ["Submitted", "Under Review"].includes(r.status),
                )
              : rows
          }
          columns={schema.columns}
          lookups={lookups}
          onOpen={(r) => setDetail(r)}
        />
      )}
      {editing && (
        <Modal
          title={(editing.id ? "Edit " : "New ") + schema.title}
          onClose={() => setEditing(null)}
        >
          <Form
            key={resource + editing.id}
            resource={resource}
            initial={editing}
            lookups={lookups}
            admin={admin}
            departmentId={user.departmentId}
            onCancel={() => setEditing(null)}
            onSave={async (v) => {
              await api(
                resource === "payments"
                  ? "workflows/payments"
                  : "records/" +
                      resource +
                      (editing.id ? "/" + editing.id : ""),
                editing.id ? "PUT" : "POST",
                v,
              );
              setEditing(null);
              await load();
              await refresh();
              notify("Record saved successfully");
            }}
          />
        </Modal>
      )}
      {detail && (
        <Modal
          title={
            (detail.name || detail.purpose || schema.title) + " · #" + detail.id
          }
          onClose={() => setDetail(null)}
        >
          <div className="detail-grid">
            {Object.entries(detail)
              .filter(
                ([k, v]) =>
                  typeof v !== "object" &&
                  !["passwordHash", "relativePath"].includes(k),
              )
              .map(([k, v]) => (
                <div key={k}>
                  <small>{label(k)}</small>
                  <strong>{value(k, v, lookups)}</strong>
                </div>
              ))}
          </div>
          {detail.restricted && (
            <div className="attention warning">
              Restricted funds: use only for {detail.purpose}.
            </div>
          )}
          {detail.items && (
            <Table
              rows={detail.items}
              columns={["description", "quantity", "unitPrice"]}
              lookups={lookups}
            />
          )}
          <div className="actions detail-actions">
            {schema.editable &&
              (admin || resource === "requests") &&
              (resource !== "requests" ||
                ["Draft", "Returned for Changes"].includes(detail.status)) && (
                <button
                  onClick={() => {
                    setEditing(detail);
                    setDetail(null);
                  }}
                >
                  Edit record
                </button>
              )}
            {resource === "suppliers" && (
              <>
                <button onClick={() => (location.hash = "payments")}>
                  Payment history
                </button>
                <button onClick={() => (location.hash = "invoices")}>
                  Invoices
                </button>
              </>
            )}
            {resource === "departments" && (
              <>
                <button
                  onClick={() =>
                    (location.hash = "budgets?departmentId=" + detail.id)
                  }
                >
                  Budgets
                </button>
                <button
                  onClick={() =>
                    (location.hash = "requests?departmentId=" + detail.id)
                  }
                >
                  Requests
                </button>
                <button
                  onClick={() =>
                    (location.hash = "expenses?departmentId=" + detail.id)
                  }
                >
                  Expenses
                </button>
                {admin && (
                  <button
                    onClick={() =>
                      (location.hash = "users?departmentId=" + detail.id)
                    }
                  >
                    Users
                  </button>
                )}
              </>
            )}
            {resource === "inventory" &&
              admin &&
              detail.category === "Permanent Items" && (
                <button
                  disabled={detail.quantity < 1}
                  onClick={() => setConvert(true)}
                >
                  Assign one unit as asset
                </button>
              )}
            {resource === "documents" && (
              <a
                className="button primary"
                href={"/api/documents/" + detail.id}
              >
                Download evidence
              </a>
            )}
          </div>
          {resource === "petty-requests" &&
            admin &&
            detail.status === "Pending" && (
              <section className="workflow">
                <h3>Funding decision</h3>
                <label>
                  Decision comments / cash issue reference
                  <textarea
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                  />
                </label>
                <div className="actions">
                  <button
                    className="primary"
                    disabled={working || !comment.trim()}
                    onClick={() =>
                      action(() =>
                        api(
                          "workflows/petty-requests/" + detail.id + "/decision",
                          "POST",
                          { approve: true, comment },
                        ),
                      )
                    }
                  >
                    Approve & replenish
                  </button>
                  <button
                    disabled={working || !comment.trim()}
                    onClick={() =>
                      action(() =>
                        api(
                          "workflows/petty-requests/" + detail.id + "/decision",
                          "POST",
                          { approve: false, comment },
                        ),
                      )
                    }
                  >
                    Reject request
                  </button>
                </div>
              </section>
            )}
          {resource === "requests" && (
            <section className="workflow">
              <h3>Move this request forward</h3>
              <p>
                Approval reserves funds. Payments are recorded separately after
                receipt and invoice.
              </p>
              <div className="form-grid">
                <label>
                  Next stage
                  <select
                    value={decision}
                    onChange={(e) => setDecision(e.target.value)}
                  >
                    <option value="">Select action…</option>
                    {transitions
                      .filter(
                        (s) => admin || ["Submitted", "Cancelled"].includes(s),
                      )
                      .map((s) => (
                        <option key={s}>{s}</option>
                      ))}
                  </select>
                </label>
                {decision === "Approved" && (
                  <>
                    <label>
                      Approved amount (KES)
                      <input
                        type="number"
                        step=".01"
                        value={amount}
                        onChange={(e) => setAmount(e.target.value)}
                      />
                    </label>
                    <label>
                      Supplier
                      <select
                        value={supplier}
                        onChange={(e) => setSupplier(e.target.value)}
                      >
                        <option value="">Select…</option>
                        {lookups.suppliers?.map((s) => (
                          <option key={s.id} value={s.id}>
                            {s.name}
                          </option>
                        ))}
                      </select>
                    </label>
                  </>
                )}
                <label className="wide">
                  Decision comments / override reason
                  <textarea
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                  />
                </label>
              </div>
              <button
                className="primary"
                disabled={!decision || working}
                onClick={() =>
                  action(() =>
                    api("workflows/requests/" + detail.id + "/status", "POST", {
                      status: decision,
                      comment,
                      amount: amount ? Number(amount) : null,
                      supplierId: supplier ? Number(supplier) : null,
                    }),
                  )
                }
              >
                {working ? "Saving…" : "Confirm decision"}
              </button>
              {admin &&
                ["Received", "Partially Received"].includes(detail.status) && (
                  <div className="actions">
                    <button onClick={() => (location.hash = "invoices")}>
                      Record invoice
                    </button>
                    <button onClick={() => (location.hash = "payments")}>
                      Record payment
                    </button>
                  </div>
                )}
            </section>
          )}
          {[
            "requests",
            "payments",
            "invoices",
            "expenses",
            "petty-transactions",
            "income",
            "assets",
            "inventory",
          ].includes(resource) && (
            <section className="workflow">
              <h3>Supporting evidence</h3>
              {docs.map((d) => (
                <a
                  className="document-link"
                  key={d.id}
                  href={"/api/documents/" + d.id}
                >
                  {d.kind} · {d.name} ↗
                </a>
              ))}
              {!docs.length && (
                <p className="evidence-missing">
                  No supporting documents attached yet.
                </p>
              )}
              <div className="upload">
                <select
                  aria-label="Document type"
                  value={kind}
                  onChange={(e) => setKind(e.target.value)}
                >
                  {[
                    "Receipt",
                    "Invoice",
                    "Quotation",
                    "Delivery Note",
                    "Payment Proof",
                    "Donation Letter",
                    "Grant Document",
                    "Other",
                  ].map((k) => (
                    <option key={k}>{k}</option>
                  ))}
                </select>
                <input
                  aria-label="Evidence file"
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png"
                  onChange={(e) => setFile(e.target.files?.[0])}
                />
                <button
                  disabled={!file || working}
                  onClick={() =>
                    action(async () => {
                      const form = new FormData();
                      form.append("entity", resource);
                      form.append("entityId", detail.id);
                      form.append("kind", kind);
                      form.append("file", file!);
                      await api("documents", "POST", form);
                    })
                  }
                >
                  Attach document
                </button>
              </div>
              <small>
                PDF, JPG or PNG · Maximum 10 MB · Department access protected
              </small>
            </section>
          )}
          {Object.entries(related).map(([key, records]) => (
            <section className="workflow" key={key}>
              <h3>{schemas[key].title}</h3>
              <Table
                rows={records}
                columns={schemas[key].columns}
                lookups={lookups}
              />
            </section>
          ))}
          <section className="workflow">
            <h3>Transaction history</h3>
            {history.length ? (
              history.map((h) => (
                <div className="history" key={h.id}>
                  <strong>{h.action}</strong>
                  <p>{h.description}</p>
                  <small>
                    {h.actor} · {new Date(h.createdAt).toLocaleString("en-GB")}
                  </small>
                  {h.oldValues && (
                    <details>
                      <summary>Previous values</summary>
                      <pre>{h.oldValues}</pre>
                    </details>
                  )}
                </div>
              ))
            ) : (
              <p className="muted">No recorded changes yet.</p>
            )}
          </section>
          {error && <p className="error">{error}</p>}
        </Modal>
      )}
      {convert && detail && (
        <Modal
          title="Assign permanent stock as an asset"
          onClose={() => setConvert(false)}
        >
          <Form
            resource="assets"
            initial={{
              name: detail.name,
              departmentId: detail.departmentId,
              source: "Donation",
              acquisitionCost: 0,
              category: detail.category,
            }}
            lookups={lookups}
            admin={admin}
            onCancel={() => setConvert(false)}
            onSave={async (v) => {
              await api(
                "workflows/inventory/" + detail.id + "/asset",
                "POST",
                v,
              );
              setConvert(false);
              setDetail(null);
              await load();
              await refresh();
              notify("One stock unit assigned to asset register");
            }}
          />
        </Modal>
      )}
    </>
  );
}
