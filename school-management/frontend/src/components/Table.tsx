import { useState } from "react";
import { Row, date, kes } from "../api";
import { moneyKeys, relation } from "../schema";
export function value(
  key: string,
  v: any,
  lookups: Record<string, Row[]>,
): string {
  if (v === null || v === undefined) return "—";
  if (moneyKeys.includes(key)) return kes(v);
  if (relation[key])
    return (
      lookups[relation[key]]?.find((r) => r.id === v)?.name ||
      lookups[relation[key]]?.find((r) => r.id === v)?.purpose ||
      "#" + v
    );
  if (
    key.toLowerCase().includes("date") ||
    key === "createdAt" ||
    key === "requiredBy"
  )
    return date(v);
  if (typeof v === "boolean") return v ? "Yes" : "No";
  if (typeof v === "object") return JSON.stringify(v);
  return String(v);
}
export const label = (key: string) =>
  key
    .replace(/Id$/, "")
    .replace(/([A-Z])/g, " $1")
    .replace(/^./, (c) => c.toUpperCase());
export function Table({
  rows,
  columns,
  lookups,
  onOpen,
  report = false,
}: {
  rows: Row[];
  columns: string[];
  lookups: Record<string, Row[]>;
  onOpen?: (row: Row) => void;
  report?: boolean;
}) {
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState("id");
  const [desc, setDesc] = useState(true);
  const [dept, setDept] = useState(
    new URLSearchParams(location.hash.split("?")[1]).get("departmentId") || "",
  );
  const [supplier, setSupplier] = useState(
    new URLSearchParams(location.hash.split("?")[1]).get("supplierId") || "",
  );
  const [status, setStatus] = useState("");
  const filtered = rows
    .filter(
      (r) =>
        (!supplier || String(r.supplierId) === supplier) &&
        (!dept || String(r.departmentId) === dept) &&
        (!status || r.status === status) &&
        (!q ||
          columns.some((k) =>
            value(k, r[k], lookups).toLowerCase().includes(q.toLowerCase()),
          )),
    )
    .sort((a, b) =>
      typeof a[sort] === "number"
        ? (a[sort] - b[sort]) * (desc ? -1 : 1)
        : String(a[sort] ?? "").localeCompare(String(b[sort] ?? "")) *
          (desc ? -1 : 1),
    );
  const pages = Math.max(1, Math.ceil(filtered.length / 10));
  const current = Math.min(page, pages);
  return (
    <div className="table-panel">
      {!report && (
        <div className="table-tools">
          <input
            aria-label="Search records"
            placeholder="Search records…"
            value={q}
            onChange={(e) => {
              setQ(e.target.value);
              setPage(1);
            }}
          />
          {columns.includes("departmentId") && (
            <select
              aria-label="Filter department"
              value={dept}
              onChange={(e) => {
                setDept(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All departments</option>
              {lookups.departments?.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          )}
          {columns.includes("supplierId") && (
            <select
              aria-label="Filter supplier"
              value={supplier}
              onChange={(e) => setSupplier(e.target.value)}
            >
              <option value="">All suppliers</option>
              {lookups.suppliers?.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
          )}
          {columns.includes("status") && (
            <select
              aria-label="Filter status"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <option value="">All statuses</option>
              {[...new Set(rows.map((r) => r.status))].map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          )}
          <span>{filtered.length} records</span>
        </div>
      )}
      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              {columns.map((k) => (
                <th key={k}>
                  <button
                    onClick={() => {
                      setSort(k);
                      setDesc(sort === k ? !desc : false);
                    }}
                  >
                    {label(k)} {sort === k ? (desc ? "↓" : "↑") : ""}
                  </button>
                </th>
              ))}
              {onOpen && <th />}
            </tr>
          </thead>
          <tbody>
            {filtered.slice((current - 1) * 10, current * 10).map((r, i) => (
              <tr key={r.id ?? i}>
                {columns.map((k) => (
                  <td key={k}>
                    {k === "status" || k === "decision" ? (
                      <span
                        className={"badge " + String(r[k]).replaceAll(" ", "-")}
                      >
                        {r[k]}
                      </span>
                    ) : k === "missingDocuments" ? (
                      <span className={r[k] ? "evidence-missing" : "muted"}>
                        {r[k] ? "Evidence needed" : "Attached"}
                      </span>
                    ) : (
                      value(k, r[k], lookups)
                    )}
                  </td>
                ))}
                {onOpen && (
                  <td>
                    <button className="link" onClick={() => onOpen(r)}>
                      Open ↗
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {!filtered.length && (
          <div className="empty">
            <strong>No records found</strong>
            <p>Create a record or adjust your filters.</p>
          </div>
        )}
      </div>
      <div className="pagination">
        <span>
          Page {current} of {pages}
        </span>
        <button disabled={current <= 1} onClick={() => setPage(current - 1)}>
          Previous
        </button>
        <button
          disabled={current >= pages}
          onClick={() => setPage(current + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
