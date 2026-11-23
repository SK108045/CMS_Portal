import { useCallback, useEffect, useRef, useState } from "react";
import { Check, ClipboardCheck, ArrowUpRight, RefreshCw } from "lucide-react";
import { api, date, kes } from "../api";
import { Modal } from "./Modal";

interface ApprovalRequest {
  id: number;
  purpose: string;
  description: string;
  notes: string;
  department: string;
  requester: string;
  estimatedAmount: number;
  budget: string;
  availableBudget: number;
  supplierId: number | null;
  supplier: string | null;
  status: string;
  priority: string;
  requiredBy: string;
  selfApproval: boolean;
  canQuickApprove: boolean;
  reviewReasons: string[];
  items: { description: string; quantity: number; unitPrice: number }[];
  documents: { id: number; name: string; kind: string }[];
}
interface Queue {
  items: ApprovalRequest[];
  totalCount: number;
  totalAmount: number;
  draftCount: number;
  suppliers: { id: number; name: string }[];
}
type Decision = "Approved" | "Rejected" | "Returned for Changes";

export function ApprovalQueue({
  compact = false,
  onChanged,
  notify,
}: {
  compact?: boolean;
  onChanged: () => Promise<void>;
  notify: (message: string) => void;
}) {
  const [queue, setQueue] = useState<Queue>();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const saving = useRef(false);
  const [selection, setSelection] = useState<{
    request: ApprovalRequest;
    decision: Decision;
  }>();
  const [amount, setAmount] = useState("");
  const [supplier, setSupplier] = useState("");
  const [reason, setReason] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const load = useCallback(async () => {
    const data: Queue = await api("insights/approval-queue");
    setQueue(data);
  }, []);
  useEffect(() => {
    let active = true;
    const refresh = () =>
      api("insights/approval-queue")
        .then((data: Queue) => {
          if (active) setQueue(data);
        })
        .catch((e: Error) => {
          if (active) setError(e.message);
        });
    refresh();
    window.addEventListener("requests-changed", refresh);
    window.addEventListener("focus", refresh);
    return () => {
      active = false;
      window.removeEventListener("requests-changed", refresh);
      window.removeEventListener("focus", refresh);
    };
  }, []);

  function review(request: ApprovalRequest, decision: Decision = "Approved") {
    setSelection({ request, decision });
    setAmount(String(request.estimatedAmount));
    setSupplier(String(request.supplierId ?? ""));
    setReason("");
    setError("");
  }
  async function decide(
    request: ApprovalRequest,
    decision: Decision,
    comment: string,
    approvedAmount = request.estimatedAmount,
    supplierId = request.supplierId,
  ) {
    if (saving.current) return;
    saving.current = true;
    setBusy(true);
    setError("");
    try {
      await api(`workflows/requests/${request.id}/status`, "POST", {
        status: decision,
        comment,
        amount: approvedAmount,
        supplierId,
      });
      setSelection(undefined);
      notify(
        `${request.purpose}: ${decision === "Returned for Changes" ? "returned for changes" : decision.toLowerCase()}.`,
      );
      // Refresh both the decision queue and the dashboard's commitments from PostgreSQL.
      await Promise.all([load(), onChanged()]);
    } catch (e) {
      setError((e as Error).message);
      await load().catch(() => {});
    } finally {
      saving.current = false;
      setBusy(false);
    }
  }
  const matching =
    queue?.items.filter((r) =>
      `${r.purpose} ${r.department} ${r.requester} ${r.supplier ?? ""}`
        .toLowerCase()
        .includes(search.toLowerCase()),
    ) ?? [];
  const pages = Math.max(1, Math.ceil(matching.length / 8));
  const current = Math.min(page, pages);
  const visible = compact
    ? matching.slice(0, 3)
    : matching.slice((current - 1) * 8, current * 8);
  const approving = selection?.decision === "Approved";
  const needsReason =
    !approving ||
    !!selection?.request.selfApproval ||
    Number(amount) > (selection?.request.availableBudget ?? 0);
  return (
    <section
      className="approval-inbox"
      aria-label="Purchase request approvals"
      aria-busy={busy}
    >
      <div className="approval-heading">
        <div className="approval-heading-title">
          <span className="approval-icon">
            <ClipboardCheck size={24} />
          </span>
          <div>
            <span className="eyebrow">YOUR DECISIONS COME FIRST</span>
            <h2>
              Awaiting your approval{" "}
              {queue && (
                <span className="approval-count">{queue.totalCount}</span>
              )}
            </h2>
            <p>
              Approve a request here. Funds are committed; payment is recorded
              separately.
            </p>
          </div>
        </div>
        <div className="approval-summary">
          {queue && (
            <>
              <small>Total requested</small>
              <strong>{kes(queue.totalAmount)}</strong>
            </>
          )}
          {compact ? (
            <a className="approval-all" href="#approvals">
              Approval inbox <ArrowUpRight size={14} />
            </a>
          ) : (
            <button
              disabled={busy}
              onClick={() => load().catch((e) => setError(e.message))}
            >
              <RefreshCw size={14} /> Refresh
            </button>
          )}
        </div>
      </div>
      {error && !selection && (
        <div className="error" role="alert">
          {error}
          <button
            onClick={() => {
              setError("");
              load().catch((e) => setError(e.message));
            }}
          >
            Retry
          </button>
        </div>
      )}
      {!queue ? (
        <p className="approval-empty" role="status">
          Loading requests awaiting your decision…
        </p>
      ) : (
        <>
          {!compact && queue.totalCount > 0 && (
            <div className="approval-search">
              <input
                aria-label="Search approval inbox"
                placeholder="Find a request, department or requester…"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
              />
            </div>
          )}
          <div className="approval-list">
            {visible.map((r) => (
              <article
                className="approval-request"
                key={r.id}
                data-request-id={r.id}
                aria-label={r.purpose}
              >
                <div className="approval-request-main">
                  <div className="approval-request-meta">
                    <span>PR-{String(r.id).padStart(4, "0")}</span>
                    <span className={"badge " + r.status.replaceAll(" ", "-")}>
                      {r.status}
                    </span>
                    {r.priority !== "Normal" && (
                      <span className="approval-priority">{r.priority}</span>
                    )}
                  </div>
                  <h3>{r.purpose}</h3>
                  <p>
                    {r.department} · {r.requester} · Needed {date(r.requiredBy)}
                  </p>
                  <small>
                    {r.supplier || "Supplier needed"} · {kes(r.availableBudget)}{" "}
                    available in budget
                  </small>
                  {r.reviewReasons.length > 0 && (
                    <small className="approval-review-note">
                      {r.reviewReasons.join(" ")}
                    </small>
                  )}
                </div>
                <div className="approval-request-decision">
                  <strong>{kes(r.estimatedAmount)}</strong>
                  <button
                    className="primary approve-button"
                    disabled={busy}
                    onClick={() =>
                      r.canQuickApprove
                        ? void decide(r, "Approved", "")
                        : review(r)
                    }
                  >
                    <Check size={17} />
                    {busy ? "Please wait…" : "Approve"}
                  </button>
                  <div className="approval-secondary">
                    <button disabled={busy} onClick={() => review(r)}>
                      Review / adjust
                    </button>
                    <button
                      disabled={busy}
                      onClick={() => review(r, "Returned for Changes")}
                    >
                      Return
                    </button>
                    <button
                      disabled={busy}
                      onClick={() => review(r, "Rejected")}
                    >
                      Reject
                    </button>
                  </div>
                </div>
              </article>
            ))}
          </div>
          {!matching.length && (
            <div className="approval-empty">
              <Check size={24} />
              <strong>
                {search
                  ? "No matching requests"
                  : "No requests waiting for approval"}
              </strong>
              <span>
                {search
                  ? "Try a different search."
                  : "Submitted requests will appear here, ready for your decision."}
              </span>
            </div>
          )}
          <div className="approval-footer">
            <span>
              {queue.draftCount} draft{queue.draftCount === 1 ? "" : "s"} not
              yet submitted.{" "}
              <a href="#requests?status=Draft">View drafts & submit</a>
            </span>
            {compact && queue.totalCount > 3 && (
              <a href="#approvals">
                View all {queue.totalCount} pending requests →
              </a>
            )}
          </div>
          {!compact && pages > 1 && (
            <div className="pagination">
              <span>
                Page {current} of {pages}
              </span>
              <button
                disabled={current <= 1 || busy}
                onClick={() => setPage(current - 1)}
              >
                Previous
              </button>
              <button
                disabled={current >= pages || busy}
                onClick={() => setPage(current + 1)}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
      {selection && (
        <Modal
          title={
            approving
              ? "Approve purchase request"
              : selection.decision === "Rejected"
                ? "Reject purchase request"
                : "Return request for changes"
          }
          onClose={() => {
            if (!busy) {
              setSelection(undefined);
              setError("");
            }
          }}
        >
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void decide(
                selection.request,
                selection.decision,
                reason.trim(),
                Number(amount),
                Number(supplier) || null,
              );
            }}
          >
            <h3>{selection.request.purpose}</h3>
            <p>
              {selection.request.department} · {selection.request.requester}
            </p>
            <p>{selection.request.description}</p>
            <p>
              <strong>{selection.request.budget}</strong>
              <br />
              {kes(selection.request.availableBudget)} available ·{" "}
              {kes(selection.request.estimatedAmount)} requested
            </p>
            {approving && (
              <>
                {selection.request.selfApproval && (
                  <p className="chart-warning">
                    This is your own request. Record why self-approval is
                    necessary.
                  </p>
                )}
                <div className="form-grid">
                  <label>
                    Approved amount (KES)
                    <input
                      type="number"
                      min="0.01"
                      step="0.01"
                      required
                      value={amount}
                      onChange={(e) => setAmount(e.target.value)}
                    />
                  </label>
                  <label>
                    Supplier
                    <select
                      aria-label="Supplier"
                      required
                      value={supplier}
                      onChange={(e) => setSupplier(e.target.value)}
                    >
                      <option value="">Choose a supplier</option>
                      {queue?.suppliers.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.name}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
                {Number(amount) > selection.request.availableBudget && (
                  <p className="chart-warning">
                    This exceeds the available budget. An authorised override
                    with a reason will be recorded.
                  </p>
                )}
              </>
            )}
            <label>
              {needsReason
                ? "Decision / override reason"
                : "Approval comments (optional)"}
              <textarea
                required={needsReason}
                maxLength={4000}
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </label>
            <details className="approval-details">
              <summary>Request items & supporting evidence</summary>
              <ul>
                {selection.request.items.map((i, n) => (
                  <li key={n}>
                    {i.quantity} × {i.description} · {kes(i.unitPrice)} each
                  </li>
                ))}
              </ul>
              {selection.request.notes && <p>{selection.request.notes}</p>}
              {selection.request.documents.length ? (
                selection.request.documents.map((d) => (
                  <p key={d.id}>
                    <a
                      href={`/api/documents/${d.id}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      {d.kind}: {d.name} ↗
                    </a>
                  </p>
                ))
              ) : (
                <p>No supporting documents attached to this request.</p>
              )}
            </details>
            {error && (
              <div className="error" role="alert">
                {error}
              </div>
            )}
            <div className="form-actions">
              <button
                type="button"
                disabled={busy}
                onClick={() => {
                  setSelection(undefined);
                  setError("");
                }}
              >
                Cancel
              </button>
              <button className="primary" disabled={busy}>
                {busy
                  ? "Saving decision…"
                  : approving
                    ? "Approve request"
                    : selection.decision === "Rejected"
                      ? "Reject request"
                      : "Return request"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </section>
  );
}
