import { useEffect, useState } from "react";
import { api, Row } from "../../api";
import { BarChart, ChartCard, ChartSummary, DonutChart } from "./Charts";
const supported = [
  "budgets",
  "requests",
  "payments",
  "expenses",
  "income",
  "inventory",
  "assets",
  "petty-accounts",
  "petty-transactions",
  "petty-requests",
];
export function VisualSummary({
  summary,
  caption = "Live records",
}: {
  summary: ChartSummary;
  caption?: string;
}) {
  return (
    <div className="analytics-grid module-charts">
      <ChartCard
        eyebrow={caption}
        title={summary.comparisonTitle}
        subtitle={
          summary.comparisonUnit === "quantity"
            ? "Each item uses its own unit; quantities are not added across different items."
            : summary.records + " records in this view."
        }
      >
        <BarChart
          points={summary.comparison}
          unit={summary.comparisonUnit}
          primary={summary.primaryLabel}
          secondary={summary.secondaryLabel || undefined}
          tertiary={summary.tertiaryLabel || undefined}
          limit={5}
        />
      </ChartCard>
      <ChartCard eyebrow="AT A GLANCE" title={summary.distributionTitle}>
        <DonutChart
          points={summary.distribution}
          unit={summary.distributionUnit}
          centerLabel={
            summary.distributionUnit === "count" ? "Records" : "Total"
          }
        />
      </ChartCard>
    </div>
  );
}
export function ModuleInsights({
  resource,
  rows,
}: {
  resource: string;
  rows: Row[];
}) {
  const [summary, setSummary] = useState<ChartSummary>();
  const [error, setError] = useState("");
  useEffect(() => {
    if (!supported.includes(resource)) return;
    let live = true;
    setSummary(undefined);
    api("insights/charts/" + resource)
      .then((d) => {
        if (live) {
          setSummary(d);
          setError("");
        }
      })
      .catch((e) => live && setError(e.message));
    return () => {
      live = false;
    };
  }, [resource, rows]);
  if (!supported.includes(resource)) return null;
  if (error)
    return (
      <div className="error" role="alert">
        Unable to load chart summary: {error}
      </div>
    );
  if (!summary)
    return (
      <div className="chart-loading" role="status">
        Preparing visual overview…
      </div>
    );
  return (
    <VisualSummary
      summary={summary}
      caption="MODULE OVERVIEW · ALL AUTHORISED RECORDS"
    />
  );
}
