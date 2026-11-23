import { has, read } from "../permissions";
import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  Wallet,
  ShieldCheck,
  ClipboardCheck,
  CircleDollarSign,
  TrendingUp,
} from "lucide-react";
import { api, Row, kes } from "../api";
import { ApprovalQueue } from "../components/ApprovalQueue";
import {
  BarChart,
  ChartCard,
  DonutChart,
  TrendChart,
  ChartSummary,
} from "../components/charts/Charts";
export function Dashboard({
  go,
  lookups,
  user,
  notify,
}: {
  go: (s: string) => void;
  lookups: Record<string, Row[]>;
  user: Row;
  notify: (message: string) => void;
}) {
  const [data, setData] = useState<Row>();
  const [error, setError] = useState("");
  const [fullBudget, setFullBudget] = useState(false);
  useEffect(() => {
    let live = true;
    api("insights/dashboard")
      .then((d) => live && setData(d))
      .catch((e) => live && setError(e.message));
    return () => {
      live = false;
    };
  }, []);
  if (error) return <div className="error">{error}</div>;
  if (!data)
    return <div className="loading">Loading school financial position…</div>;
  const charts = data.charts as {
    budgets: ChartSummary;
    spending: ChartSummary;
    income: ChartSummary;
    requests: ChartSummary;
  };
  const financial = read(user, "payments") && read(user, "expenses");
  const canBudget = read(user, "budgets");
  const canIncome = read(user, "income");
  const stock = data.operationalCharts as ChartSummary | undefined;
  const canApprove = has(user, "requests.approve");
  const cards = [
    ["Total budget", data.budget, "budgets", Wallet],
    ["Committed", data.committed, "requests", ClipboardCheck],
    [
      "Total spent",
      data.spent,
      user.reports?.includes("Transaction Report")
        ? "reports?name=Transaction%20Report"
        : "budgets",
      CircleDollarSign,
    ],
    ["Available balance", data.remaining, "budgets", ShieldCheck],
  ] as const;
  const budgetPoints = charts.budgets.comparison
    .map((p) => ({ ...p, tertiary: fullBudget ? p.tertiary : 0 }))
    .filter((p) => fullBudget || p.value + p.secondary > 0)
    .sort(
      (a, b) =>
        b.value + b.secondary + b.tertiary - a.value - a.secondary - a.tertiary,
    );
  const cashIncomeTotal = charts.income.distribution.reduce(
    (sum, point) => sum + point.value,
    0,
  );
  const leadingRequestStage = [...charts.requests.distribution].sort(
    (a, b) => b.value - a.value,
  )[0];
  return (
    <>
      <div className="welcome analytics-welcome">
        <div className="welcome-seal">
          <ShieldCheck size={30} />
        </div>
        <div>
          <span className="eyebrow">STEWARDSHIP / FINANCIAL OVERVIEW</span>
          <h1>
            {canApprove
              ? "Approve purchase requests"
              : "Every shilling, accounted for."}
          </h1>
          <p>
            {canApprove
              ? "Your approval inbox first. The school’s financial position below."
              : "Your school’s finances, clearly in view."}
          </p>
        </div>
        <div className="welcome-actions">
          <span className="live-data">
            <i /> Live school records
          </span>
          <button
            className="primary"
            onClick={() => go(canApprove ? "approvals" : "reports")}
          >
            {canApprove ? "Approval inbox" : "View reports"}{" "}
            <ArrowUpRight size={16} />
          </button>
        </div>
      </div>
      {canApprove && (
        <ApprovalQueue
          compact
          notify={notify}
          onChanged={async () => setData(await api("insights/dashboard"))}
        />
      )}
      <div className="cards">
        {canBudget &&
          cards.map(([name, n, path, Icon], i) => (
            <button
              key={name}
              className={"metric metric-" + i}
              onClick={() => go(path)}
            >
              <div className="metric-top">
                <span>{name}</span>
                <span className="metric-icon">
                  <Icon size={18} />
                </span>
              </div>
              <strong>{kes(n)}</strong>
              <small>
                {i === 1
                  ? "Reserved, awaiting payment"
                  : i === 3
                    ? "After spending and commitments"
                    : "Open financial records"}{" "}
                <ArrowUpRight size={12} />
              </small>
            </button>
          ))}
      </div>
      {!canBudget && data.operations && (
        <div className="cards">
          {[
            ["Inventory items", data.operations.inventoryItems, "inventory"],
            ["Approved orders", data.operations.receivingRequests, "requests"],
            ["Registered assets", data.operations.assets, "assets"],
            ["At reorder level", data.operations.lowStock, "inventory"],
          ].map(([label, n, route]) => (
            <button
              key={label}
              className="metric"
              onClick={() => go(String(route))}
            >
              <span>{label}</span>
              <strong>{n}</strong>
              <small>View authorised records ↗</small>
            </button>
          ))}
        </div>
      )}
      <div className="analytics-grid analytics-top">
        {financial ? (
          <ChartCard
            className="trend-feature"
            eyebrow="CASH FLOW"
            title="The financial year, at a glance"
            subtitle="Actual payments, direct expenses and petty cash spending against cash income."
            insight={
              <p>
                The chart compares money leaving the school with recorded cash
                coming in month by month. Total recorded spending is{" "}
                <strong>{kes(data.spent)}</strong>, while cash income shown in
                this view totals <strong>{kes(cashIncomeTotal)}</strong>. Use
                peaks to identify months that deserve a closer look.
              </p>
            }
          >
            <TrendChart
              spending={charts.spending.trend}
              income={charts.income.trend}
              onOpen={(month) =>
                go(
                  "reports?name=Transaction%20Report&from=" +
                    month +
                    "-01&to=" +
                    month +
                    "-" +
                    new Date(
                      Number(month.slice(0, 4)),
                      Number(month.slice(5)),
                      0,
                    ).getDate(),
                )
              }
            />
          </ChartCard>
        ) : (
          stock && (
            <ChartCard
              eyebrow="STORES & DEPARTMENT INVENTORY"
              title="Stock at a glance"
              subtitle="Quantities use each item's own unit."
            >
              <BarChart
                points={stock.comparison}
                unit="quantity"
                primary="Quantity"
                onSelect={() => go("inventory")}
              />
            </ChartCard>
          )
        )}
        {canBudget ? (
          <ChartCard
            eyebrow="BUDGET POSITION"
            title="Reserved. Spent. Available."
            subtitle="Unpaid approvals stay separate from money spent."
            insight={
              <p>
                <strong>{kes(data.spent)}</strong> has already been spent and{" "}
                <strong>{kes(data.committed)}</strong> is reserved for approved
                requests that are not fully paid yet. The school still has{" "}
                <strong>{kes(data.remaining)}</strong> available after both.
              </p>
            }
          >
            <DonutChart
              points={charts.budgets.distribution}
              centerLabel="Resources allocated"
              onSelect={() => go("budgets")}
            />
            {data.remaining < 0 && (
              <p className="chart-warning">
                Over budget by {kes(-data.remaining)}. Available balance is
                shown as zero in the chart.
              </p>
            )}
          </ChartCard>
        ) : (
          stock && (
            <ChartCard
              eyebrow="INVENTORY CLASSIFICATIONS"
              title="Resources in stores"
            >
              <DonutChart
                points={stock.distribution}
                unit="count"
                centerLabel="Inventory items"
                onSelect={() => go("inventory")}
              />
            </ChartCard>
          )
        )}
      </div>
      <div className="quick-stats analytics-stats">
        {[
          [
            "Awaiting approval",
            data.pending,
            canApprove ? "approvals" : "requests",
          ],
          [
            "Evidence needed",
            data.missingEvidence,
            "reports?name=Missing%20Documents%20Report",
          ],
          ["Petty cash balance", kes(data.pettyCash), "petty-accounts"],
          ["Donations & grants", kes(data.donations), "income"],
          ["Other income", kes(data.otherIncome), "income"],
        ]
          .filter(([, , path]) =>
            path === "requests" || path === "approvals"
              ? read(user, "requests")
              : String(path).startsWith("reports")
                ? financial
                : path === "petty-accounts"
                  ? has(user, "petty.view")
                  : canIncome,
          )
          .map(([label, n, path]) => (
            <button key={label} onClick={() => go(String(path))}>
              <span>{label}</span>
              <strong>{n}</strong>
              <ArrowUpRight size={13} />
            </button>
          ))}
      </div>
      <div
        className="analytics-grid analytics-middle"
        style={{
          gridTemplateColumns: canBudget && canIncome ? undefined : "1fr",
        }}
      >
        {canBudget && (
          <ChartCard
            eyebrow="DEPARTMENT VIEW"
            title="Where resources are going"
            subtitle={
              fullBudget
                ? "Full allocation, split into spent, committed and available."
                : "Compare actual spending and unpaid commitments."
            }
            insight={
              <p>
                Each row shows how much a department has already used and how
                much is tied up in approved purchases. Longer combined bars
                mean more of that department's allocation is already spoken
                for, making pressure points easy to spot.
              </p>
            }
            action={
              <div className="chart-toggle">
                <button
                  aria-pressed={!fullBudget}
                  onClick={() => setFullBudget(false)}
                >
                  Used funds
                </button>
                <button
                  aria-pressed={fullBudget}
                  onClick={() => setFullBudget(true)}
                >
                  Full budget
                </button>
              </div>
            }
          >
            <BarChart
              points={budgetPoints}
              primary="Spent"
              secondary="Committed"
              tertiary={fullBudget ? "Available" : undefined}
              onSelect={(p) =>
                go("budgets?departmentId=" + (p.key === "school" ? "" : p.key))
              }
            />
          </ChartCard>
        )}
        {canIncome && (
          <ChartCard
            eyebrow="SUPPORTING THE MISSION"
            title="Income & giving"
            subtitle="Cash donations, grants and other income. Non-cash gifts are tracked in inventory."
            insight={
              <p>
                This breaks down the school's recorded cash support by source
                type. A large slice means the school is relying more heavily
                on that source, while restricted donations should still be
                checked against their stated purpose before use.
              </p>
            }
          >
            <DonutChart
              points={charts.income.distribution}
              centerLabel="Cash received"
              onSelect={() => go("income")}
            />
            <button className="chart-text-link" onClick={() => go("income")}>
              Explore income & donations <ArrowUpRight size={14} />
            </button>
          </ChartCard>
        )}
      </div>
      <div className="analytics-grid analytics-bottom">
        <ChartCard
          eyebrow="PURCHASE PIPELINE"
          title="From need to fulfilment"
          subtitle="Every request, grouped by its current stage."
          insight={
            <p>
              There are <strong>{charts.requests.records}</strong> purchase
              requests in this view.{" "}
              {leadingRequestStage ? (
                <>
                  The largest stage is{" "}
                  <strong>{leadingRequestStage.label}</strong> with{" "}
                  <strong>{leadingRequestStage.value}</strong> requests. If
                  Draft or Submitted dominates, that is a sign requests are
                  waiting to move forward.
                </>
              ) : (
                <>No purchase requests have been recorded yet.</>
              )}
            </p>
          }
        >
          <DonutChart
            points={charts.requests.distribution}
            unit="count"
            centerLabel="Purchase requests"
            onSelect={(p) => go("requests?status=" + encodeURIComponent(p.key))}
          />
        </ChartCard>
        <ChartCard
          eyebrow="MANAGEMENT ATTENTION"
          title="Keep the trail complete"
          subtitle="Follow up on evidence, approvals and budget pressure."
        >
          <div className="attention-list">
            {data.missingEvidence > 0 && (
              <button
                className="attention"
                onClick={() => go("reports?name=Missing%20Documents%20Report")}
              >
                <strong>
                  {data.missingEvidence} transactions need evidence
                </strong>
                <span>Attach receipts and payment confirmations ↗</span>
              </button>
            )}
            {data.pending > 0 && (
              <button
                className="attention"
                onClick={() => go(canApprove ? "approvals" : "requests")}
              >
                <strong>{data.pending} requests awaiting a decision</strong>
                <span>Review needs and budget impact ↗</span>
              </button>
            )}
            {data.budgets
              .filter((b: Row) => b.remaining <= b.amount * 0.25)
              .map((b: Row) => (
                <button
                  className="attention warning"
                  key={b.id}
                  onClick={() =>
                    go("budgets?departmentId=" + (b.departmentId || ""))
                  }
                >
                  <strong>{b.name}</strong>
                  <span>
                    {b.remaining <= 0
                      ? "Budget exhausted"
                      : b.remaining <= b.amount * 0.1
                        ? "90% or more allocated"
                        : "75% or more allocated"}{" "}
                    · {kes(b.remaining)} available
                  </span>
                </button>
              ))}
            {data.missingEvidence === 0 &&
              data.pending === 0 &&
              !data.budgets.some(
                (b: Row) => b.remaining <= b.amount * 0.25,
              ) && (
                <div className="chart-clear">
                  <ShieldCheck size={32} />
                  <strong>No outstanding alerts</strong>
                  <p>Continue recording evidence as you go.</p>
                </div>
              )}
          </div>
        </ChartCard>
      </div>
      <section className="recent-section">
        <div className="section-heading">
          <div>
            <span className="chart-eyebrow">LATEST ACTIVITY</span>
            <h2>Recent purchase requests</h2>
          </div>
          <button className="chart-text-link" onClick={() => go("requests")}>
            View all requests <ArrowUpRight size={14} />
          </button>
        </div>
        <div className="request-card-grid">
          {data.recentRequests.slice(0, 3).map((r: Row) => (
            <button
              className="request-preview"
              key={r.id}
              onClick={() => go("requests")}
            >
              <div>
                <span className="request-number">
                  PR-{String(r.id).padStart(4, "0")}
                </span>
                <span className={"badge " + r.status.replaceAll(" ", "-")}>
                  {r.status}
                </span>
              </div>
              <strong>{r.purpose}</strong>
              <small>
                {lookups.departments?.find((d) => d.id === r.departmentId)
                  ?.name || "Department request"}
              </small>
              <footer>
                <span>
                  {canBudget ? kes(r.estimatedAmount) : "Receiving order"}
                </span>
                <ArrowUpRight size={17} />
              </footer>
            </button>
          ))}
        </div>
      </section>
      <div className="analytics-footnote">
        <TrendingUp size={14} />
        <span>
          Charts use live, authorised school records. Select a chart segment or
          bar to explore the underlying records.
        </span>
      </div>
    </>
  );
}
