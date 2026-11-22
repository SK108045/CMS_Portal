import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  Wallet,
  ShieldCheck,
  ClipboardCheck,
  CircleDollarSign,
} from "lucide-react";
import { api, Row, kes } from "../api";
export function Dashboard({
  go,
  lookups,
}: {
  go: (s: string) => void;
  lookups: Record<string, Row[]>;
}) {
  const [data, setData] = useState<Row>();
  const [error, setError] = useState("");
  useEffect(() => {
    api("insights/dashboard")
      .then(setData)
      .catch((e) => setError(e.message));
  }, []);
  if (error) return <div className="error">{error}</div>;
  if (!data)
    return <div className="loading">Loading school financial position…</div>;
  const cards = [
    ["Total budget", data.budget, "budgets"],
    ["Committed", data.committed, "requests"],
    ["Total spent", data.spent, "payments"],
    ["Available balance", data.remaining, "budgets"],
  ];
  const used = Math.min(
    100,
    data.budget ? ((data.spent + data.committed) / data.budget) * 100 : 0,
  );
  return (
    <>
      <div className="welcome">
        <div className="welcome-seal">
          <ShieldCheck size={30} />
        </div>
        <div>
          <span className="eyebrow">STEWARDSHIP / FINANCIAL OVERVIEW</span>
          <h1>Every shilling, accounted for.</h1>
          <p>A clear view of the resources entrusted to our school.</p>
        </div>
        <div className="welcome-actions">
          <span className="period-tag">● Financial overview</span>
          <button className="primary" onClick={() => go("reports")}>
            View reports <ArrowUpRight size={16} />
          </button>
        </div>
      </div>
      <div className="cards">
        {cards.map(([name, n, path], i) => (
          <button
            key={name}
            className={"metric metric-" + i}
            onClick={() => go(String(path))}
          >
            <div className="metric-top">
              <span>{name}</span>
              <span className="metric-icon">
                {i === 0 ? (
                  <Wallet size={17} />
                ) : i === 1 ? (
                  <ClipboardCheck size={17} />
                ) : i === 2 ? (
                  <CircleDollarSign size={17} />
                ) : (
                  <ShieldCheck size={17} />
                )}
              </span>
            </div>
            <strong>{kes(n)}</strong>
            <small>
              {i === 1
                ? "Approved, awaiting payment"
                : i === 3
                  ? "After commitments and spending"
                  : "View underlying records"}{" "}
              ↗
            </small>
          </button>
        ))}
      </div>
      <div className="quick-stats">
        {[
          ["Awaiting approval", data.pending, "requests"],
          [
            "Evidence needed",
            data.missingEvidence,
            "reports?name=Missing%20Documents%20Report",
          ],
          ["Petty cash balance", kes(data.pettyCash), "petty-accounts"],
          ["Donations & grants", kes(data.donations), "income"],
          ["Other income", kes(data.otherIncome), "income"],
        ].map(([s, n, path]) => (
          <button key={s} onClick={() => go(String(path))}>
            <span>{s}</span>
            <strong>{n}</strong>
          </button>
        ))}
      </div>
      <div className="position-banner">
        <div>
          <span className="eyebrow">THE SCHOOL’S FINANCIAL POSITION</span>
          <h2>Resources with a purpose.</h2>
          <p>Committed funds are reserved. Spent funds have been paid.</p>
          <div className="position-key">
            <span>● Paid {kes(data.spent)}</span>
            <span>● Reserved {kes(data.committed)}</span>
            <span>● Available {kes(data.remaining)}</span>
          </div>
        </div>
        <div
          className="budget-donut"
          role="img"
          aria-label={used.toFixed(1) + " percent of budget used"}
          style={{
            background: `conic-gradient(#416b52 0 ${data.budget ? (data.spent / data.budget) * 100 : 0}%, #b7bc8b ${data.budget ? (data.spent / data.budget) * 100 : 0}% ${used}%, #e7ece1 ${used}% 100%)`,
          }}
        >
          <div>
            <strong>
              {used.toFixed(1)}
              <small>%</small>
            </strong>
            <span>budget utilised</span>
          </div>
        </div>
      </div>
      <div className="dashboard-grid">
        <section className="panel">
          <div className="section-heading">
            <div>
              <h2>Budget health</h2>
              <p>Committed and spent against each allocation</p>
            </div>
            <button className="link" onClick={() => go("budgets")}>
              All budgets ↗
            </button>
          </div>
          <div className="budget-list">
            {data.budgets.map((b: Row) => {
              const percent = b.amount
                ? ((b.spent + b.committed) / b.amount) * 100
                : 0;
              return (
                <button
                  className="budget-line"
                  key={b.id}
                  onClick={() => go("budgets")}
                >
                  <div>
                    <strong>
                      {lookups.departments?.find((d) => d.id === b.departmentId)
                        ?.name || b.name}
                    </strong>
                    <span className={percent >= 90 ? "danger-text" : ""}>
                      {percent.toFixed(0)}% used{" "}
                      {percent >= 90
                        ? " · Action needed"
                        : percent >= 75
                          ? " · Monitor"
                          : ""}
                    </span>
                  </div>
                  <div className="bar">
                    <i
                      style={{
                        width: Math.min(100, (b.spent / b.amount) * 100) + "%",
                      }}
                    />
                    <em
                      style={{
                        width:
                          Math.min(
                            100 - (b.spent / b.amount) * 100,
                            (b.committed / b.amount) * 100,
                          ) + "%",
                      }}
                    />
                  </div>
                  <small>
                    {kes(b.remaining)} available of {kes(b.amount)}
                  </small>
                </button>
              );
            })}
          </div>
          <div className="legend">
            <span>● Spent</span>
            <span>● Committed</span>
          </div>
        </section>
        <section className="panel">
          <div className="section-heading">
            <div>
              <h2>Attention & stewardship</h2>
              <p>Keep the financial trail complete</p>
            </div>
          </div>
          {data.missingEvidence > 0 && (
            <button className="attention" onClick={() => go("reports")}>
              <strong>{data.missingEvidence} transactions need evidence</strong>
              <span>Attach receipts and payment confirmations ↗</span>
            </button>
          )}
          {data.pending > 0 && (
            <button className="attention" onClick={() => go("requests")}>
              <strong>{data.pending} requests awaiting a decision</strong>
              <span>Review departmental needs and budget impact ↗</span>
            </button>
          )}
          {data.budgets
            .filter((b: Row) => b.remaining <= b.amount * 0.1)
            .map((b: Row) => (
              <button
                className="attention warning"
                key={b.id}
                onClick={() => go("budgets")}
              >
                <strong>{b.name}</strong>
                <span>
                  {b.remaining < 0 ? "Over budget" : "Approaching budget limit"}{" "}
                  · {kes(b.remaining)} remaining
                </span>
              </button>
            ))}
          <h3>Spending trend</h3>
          {!data.trend.length && (
            <p className="muted">
              Spending will appear here as payments are recorded.
            </p>
          )}
          {data.trend.map((t: Row) => (
            <div className="trend" key={t.month}>
              <span>{t.month}</span>
              <div
                style={{
                  width:
                    Math.max(
                      3,
                      (t.amount /
                        Math.max(...data.trend.map((x: Row) => x.amount))) *
                        65,
                    ) + "%",
                }}
              />
              <strong>{kes(t.amount)}</strong>
            </div>
          ))}
        </section>
      </div>
      <section className="panel">
        <div className="section-heading">
          <div>
            <h2>Recent purchase requests</h2>
            <p>Follow the needs of your school community</p>
          </div>
          <button className="link" onClick={() => go("requests")}>
            View requests ↗
          </button>
        </div>
        {data.recentRequests.map((r: Row) => (
          <button className="recent" key={r.id} onClick={() => go("requests")}>
            <span className="request-number">#{r.id}</span>
            <div>
              <strong>{r.purpose}</strong>
              <small>
                {
                  lookups.departments?.find((d) => d.id === r.departmentId)
                    ?.name
                }
              </small>
            </div>
            <span className="badge">{r.status}</span>
            <strong>{kes(r.estimatedAmount)}</strong>
          </button>
        ))}
      </section>
      <div className="dashboard-grid" style={{ marginTop: 24 }}>
        <section className="panel">
          <div className="section-heading">
            <h2>Budget usage by category</h2>
            <button className="link" onClick={() => go("reports")}>
              Category reports ↗
            </button>
          </div>
          {lookups.categories?.map((c) => {
            const rows = data.budgets.filter((b: Row) => b.categoryId === c.id);
            const allocated = rows.reduce(
              (s: number, b: Row) => s + b.amount,
              0,
            );
            if (!allocated) return null;
            const used = rows.reduce(
              (s: number, b: Row) => s + b.spent + b.committed,
              0,
            );
            return (
              <button
                className="budget-line"
                key={c.id}
                onClick={() => go("budgets")}
              >
                <div>
                  <strong>{c.name}</strong>
                  <span>
                    {kes(used)} / {kes(allocated)}
                  </span>
                </div>
                <div className="bar">
                  <i
                    style={{
                      width: Math.min(100, (used / allocated) * 100) + "%",
                    }}
                  />
                </div>
              </button>
            );
          })}
        </section>
        <section className="panel">
          <div className="section-heading">
            <h2>Recent transactions</h2>
            <button
              className="link"
              onClick={() => go("reports?name=Transaction%20Report")}
            >
              Transaction report ↗
            </button>
          </div>
          {data.recentTransactions.length ? (
            data.recentTransactions.map((t: Row, i: number) => (
              <button
                className="recent"
                key={i}
                onClick={() =>
                  go(t.purchaseRequestId ? "payments" : "expenses")
                }
              >
                <div>
                  <strong>{t.purpose || t.payee}</strong>
                  <small>
                    {t.date} · {t.method} · {t.reference}
                  </small>
                </div>
                <strong>{kes(t.amount)}</strong>
              </button>
            ))
          ) : (
            <p>No payments recorded yet.</p>
          )}
        </section>
      </div>
    </>
  );
}
