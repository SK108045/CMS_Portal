import { ChartSummary } from "../components/charts/Charts";
import { VisualSummary } from "../components/charts/ModuleInsights";
import { useEffect, useState } from "react";
import { api, Row } from "../api";
import { reports } from "../schema";
import { Table } from "../components/Table";
export function Reports({ lookups }: { lookups: Record<string, Row[]> }) {
  const [filters, setFilters] = useState<Row>({
    from: new URLSearchParams(location.hash.split("?")[1]).get("from") || "",
    to: new URLSearchParams(location.hash.split("?")[1]).get("to") || "",
    departmentId:
      new URLSearchParams(location.hash.split("?")[1]).get("departmentId") ||
      "",
    name:
      new URLSearchParams(location.hash.split("?")[1]).get("name") ||
      reports[0],
  });
  const [summary, setSummary] = useState<ChartSummary>();
  const [rows, setRows] = useState<Row[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const params = new URLSearchParams(
    Object.entries(filters)
      .filter(([, v]) => v)
      .map(([k, v]) => [k, String(v)]),
  ).toString();
  useEffect(() => {
    let live = true;
    setBusy(true);
    Promise.all([
      api("insights/reports?" + params),
      api("insights/report-charts?" + params),
    ])
      .then(([r, chart]) => {
        if (live) {
          setRows(r);
          setSummary(chart);
          setError("");
        }
      })
      .catch((e) => live && setError(e.message))
      .finally(() => live && setBusy(false));
    return () => {
      live = false;
    };
  }, [params]);
  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">ACCOUNTABILITY</span>
          <h1>Reports</h1>
          <p>
            Live records from the school database. Filter, export and print.
          </p>
        </div>
        <div className="actions">
          <a
            className="button"
            href={"/api/insights/reports?" + params + "&csv=true"}
          >
            Export CSV
          </a>
          <button onClick={() => window.print()}>Print / Save PDF</button>
        </div>
      </div>
      <div className="panel report-filters">
        <label>
          Report
          <select
            value={filters.name}
            onChange={(e) => setFilters({ ...filters, name: e.target.value })}
          >
            {reports.map((r) => (
              <option key={r}>{r}</option>
            ))}
          </select>
        </label>
        {["from", "to"].map((k) => (
          <label key={k}>
            {k === "from" ? "From date" : "To date"}
            <input
              type="date"
              value={filters[k] || ""}
              onChange={(e) => setFilters({ ...filters, [k]: e.target.value })}
            />
          </label>
        ))}
        {[
          ["departmentId", "Department", "departments"],
          ["categoryId", "Category", "categories"],
          ["supplierId", "Supplier", "suppliers"],
        ].map(([key, label, source]) => (
          <label key={key}>
            {label}
            <select
              value={filters[key] || ""}
              onChange={(e) =>
                setFilters({ ...filters, [key]: e.target.value })
              }
            >
              <option value="">All</option>
              {lookups[source]?.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </label>
        ))}
        <label>
          Status
          <input
            placeholder="e.g. Approved"
            value={filters.status || ""}
            onChange={(e) => setFilters({ ...filters, status: e.target.value })}
          />
        </label>
        <label>
          Search
          <input
            value={filters.search || ""}
            onChange={(e) => setFilters({ ...filters, search: e.target.value })}
          />
        </label>
      </div>
      {error && <p className="error">{error}</p>}
      {!busy && !error && summary && (
        <VisualSummary
          summary={summary}
          caption="FILTERED REPORT · MATCHES CSV EXPORT"
        />
      )}
      {busy ? (
        <div className="loading">Preparing report…</div>
      ) : (
        <Table
          report
          rows={rows}
          columns={
            rows.length
              ? Object.keys(rows[0])
                  .filter(
                    (k) =>
                      !["createdBy", "passwordHash", "relativePath"].includes(
                        k,
                      ) && typeof rows[0][k] !== "object",
                  )
                  .slice(0, 16)
              : ["id", "amount"]
          }
          lookups={lookups}
        />
      )}
    </>
  );
}
