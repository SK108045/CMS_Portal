import { useEffect, useId, useRef, useState, type CSSProperties } from "react";
import { Maximize2 } from "lucide-react";
import { kes } from "../../api";
import { Modal } from "../Modal";
export type Point = {
  key: string;
  label: string;
  value: number;
  secondary: number;
  tertiary: number;
};
export type Month = { monthKey: string; value: number };
export type ChartSummary = {
  comparisonTitle: string;
  distributionTitle: string;
  comparisonUnit: string;
  distributionUnit: string;
  primaryLabel: string;
  secondaryLabel?: string;
  tertiaryLabel?: string;
  comparison: Point[];
  distribution: Point[];
  trend: Month[];
  records: number;
};
export const palette = [
  "#347a61",
  "#c4a365",
  "#789eae",
  "#a5b77b",
  "#b87965",
  "#8f87ad",
  "#68a5a2",
  "#b49b8b",
];
export const compact = (n: number) =>
  new Intl.NumberFormat("en-KE", {
    notation: "compact",
    maximumFractionDigits: 1,
  }).format(n);
const format = (n: number, unit: string) =>
  unit === "money"
    ? kes(n)
    : n.toLocaleString("en-KE", { maximumFractionDigits: 2 });
function Empty({ label = "No recorded activity yet" }: { label?: string }) {
  return (
    <div className="chart-empty">
      <span className="empty-chart-icon">▥</span>
      <strong>{label}</strong>
      <span>Charts appear as records are added.</span>
    </div>
  );
}
export function ChartCard({
  eyebrow,
  title,
  subtitle,
  children,
  action,
  insight,
  expandable = true,
  className = "",
}: {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  action?: React.ReactNode;
  insight?: React.ReactNode;
  expandable?: boolean;
  className?: string;
}) {
  const [expanded, setExpanded] = useState(false);

  return (
    <>
      <section className={"chart-card chart-card-enter " + className}>
        <div className="chart-heading">
          <div>
            {eyebrow && <span className="chart-eyebrow">{eyebrow}</span>}
            <h2>{title}</h2>
            {subtitle && <p>{subtitle}</p>}
          </div>
          <div className="chart-heading-actions">
            {action}
            {expandable && (
              <button
                type="button"
                className="chart-expand-button"
                onClick={() => setExpanded(true)}
                aria-label={`Expand ${title} chart`}
                title="Expand chart"
              >
                <Maximize2 size={18} />
              </button>
            )}
          </div>
        </div>
        {children}
      </section>

      {expandable && expanded && (
        <Modal
          title={title}
          onClose={() => setExpanded(false)}
          className="analytics-focus-modal"
        >
          <div className="analytics-focus-shell">
            <div className="analytics-focus-intro">
              <div>
                {eyebrow && <span className="chart-eyebrow">{eyebrow}</span>}
                {subtitle && <p>{subtitle}</p>}
              </div>
              <span className="analytics-focus-badge">EXPANDED ANALYTICS</span>
            </div>
            <div className="analytics-focus-visual">{children}</div>
            {insight && (
              <aside className="analytics-focus-insight">
                <span>WHAT THIS TELLS YOU</span>
                <div>{insight}</div>
              </aside>
            )}
          </div>
        </Modal>
      )}
    </>
  );
}
function DataValues({
  points,
  unit,
  secondary,
  tertiary,
}: {
  points: Point[];
  unit: string;
  secondary?: string;
  tertiary?: string;
}) {
  return (
    <details className="chart-data">
      <summary>View exact values</summary>
      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Label</th>
              <th>Value</th>
              {secondary && <th>{secondary}</th>}
              {tertiary && <th>{tertiary}</th>}
            </tr>
          </thead>
          <tbody>
            {points.map((p) => (
              <tr key={p.key}>
                <td>{p.label}</td>
                <td>{format(p.value, unit)}</td>
                {secondary && <td>{format(p.secondary, unit)}</td>}
                {tertiary && <td>{format(p.tertiary, unit)}</td>}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}
export function DonutChart({
  points,
  unit = "money",
  centerLabel = "Total",
  onSelect,
}: {
  points: Point[];
  unit?: string;
  centerLabel?: string;
  onSelect?: (p: Point) => void;
}) {
  const [active, setActive] = useState<number | null>(null);
  const total = points.reduce((s, p) => s + Math.max(0, p.value), 0);
  const shown = active === null ? null : points[active];
  let offset = 0;
  const radius = 70,
    circumference = 2 * Math.PI * radius;
  if (!total) return <Empty />;
  return (
    <>
      <div className="donut-layout">
        <div className="donut-stage">
          <svg
            viewBox="0 0 190 190"
            role="img"
            aria-label={points
              .map((p) => p.label + ": " + format(p.value, unit))
              .join("; ")}
          >
            <circle
              cx="95"
              cy="95"
              r={radius}
              fill="none"
              stroke="#edf1e9"
              strokeWidth="22"
            />
            {points.map((p, i) => {
              const length = (Math.max(0, p.value) / total) * circumference;
              const position = offset;
              offset += length;
              return (
                <circle
                  key={p.key}
                  cx="95"
                  cy="95"
                  r={radius}
                  fill="none"
                  stroke={palette[i % palette.length]}
                  strokeWidth={active === i ? 28 : 22}
                  strokeDasharray={`${Math.max(0.1, length - (points.length > 1 ? 3 : 0))} ${circumference}`}
                  strokeDashoffset={-position}
                  transform="rotate(-90 95 95)"
                  className="donut-segment donut-segment-enter"
                  style={{
                    "--donut-start": String(circumference),
                    "--donut-final": String(-position),
                    animationDelay: `${i * 85}ms`,
                  } as CSSProperties}
                  onMouseEnter={() => setActive(i)}
                  onMouseLeave={() => setActive(null)}
                >
                  <title>
                    {p.label}: {format(p.value, unit)}
                  </title>
                </circle>
              );
            })}
          </svg>
          <div className="donut-center">
            <span>{shown?.label || centerLabel}</span>
            <strong>
              {unit === "money" && <small>KES </small>}
              {compact(shown?.value ?? total)}
            </strong>
            <small>
              {shown
                ? ((shown.value / total) * 100).toFixed(1) + "% of total"
                : points.length +
                  (points.length === 1
                    ? " classification"
                    : " classifications")}
            </small>
          </div>
        </div>
        <div className="chart-legend-list">
          {points.map((p, i) => (
            <button
              key={p.key}
              className={
                "chart-legend-item " + (active === i ? "active" : "")
              }
              style={{ animationDelay: `${180 + i * 65}ms` }}
              onMouseEnter={() => setActive(i)}
              onMouseLeave={() => setActive(null)}
              onFocus={() => setActive(i)}
              onBlur={() => setActive(null)}
              onClick={() => {
                setActive(i);
                onSelect?.(p);
              }}
              aria-label={
                p.label +
                ": " +
                format(p.value, unit) +
                (onSelect ? ". View records" : "")
              }
            >
              <i style={{ background: palette[i % palette.length] }} />
              <span>{p.label}</span>
              <strong>{format(p.value, unit)}</strong>
              <small>
                {((p.value / total) * 100).toFixed(
                  p.value / total < 0.05 ? 1 : 0,
                )}
                %
              </small>
            </button>
          ))}
        </div>
      </div>
      <DataValues points={points} unit={unit} />
    </>
  );
}
export function BarChart({
  points,
  unit = "money",
  primary = "Amount",
  secondary,
  tertiary,
  onSelect,
  limit = 7,
}: {
  points: Point[];
  unit?: string;
  primary?: string;
  secondary?: string;
  tertiary?: string;
  onSelect?: (p: Point) => void;
  limit?: number;
}) {
  const [hover, setHover] = useState<number | null>(null);
  const data = points.slice(0, limit);
  if (!data.length || !data.some((p) => p.value || p.secondary || p.tertiary))
    return <Empty />;
  const max = Math.max(
    ...data.map(
      (p) =>
        Math.max(0, p.value) +
        Math.max(0, p.secondary) +
        Math.max(0, p.tertiary),
    ),
    1,
  );
  const W = 660,
    left = 165,
    right = 25,
    plot = W - left - right,
    rowHeight = 40,
    H = data.length * rowHeight + 35;
  return (
    <>
      <div className="chart-series-key">
        <span>
          <i style={{ background: palette[0] }} />
          {primary}
        </span>
        {secondary && (
          <span>
            <i style={{ background: palette[1] }} />
            {secondary}
          </span>
        )}
        {tertiary && (
          <span>
            <i style={{ background: "#e1e8d9" }} />
            {tertiary}
          </span>
        )}
        <small>
          {unit === "money"
            ? "KES"
            : unit === "count"
              ? "Records"
              : "Item units"}
        </small>
      </div>
      <div className="bar-chart-wrap">
        <svg
          className="bar-chart"
          viewBox={`0 0 ${W} ${H}`}
          role="img"
          aria-label={`${primary} by ${data.map((p) => p.label).join(", ")}`}
        >
          {[0, 0.25, 0.5, 0.75, 1].map((t) => (
            <g key={t}>
              <line
                x1={left + plot * t}
                y1="0"
                x2={left + plot * t}
                y2={H - 28}
                stroke="#e8eee4"
                strokeDasharray="3 5"
              />
              <text
                x={left + plot * t}
                y={H - 9}
                textAnchor={t === 0 ? "start" : t === 1 ? "end" : "middle"}
                className="chart-tick"
              >
                {compact(max * t)}
              </text>
            </g>
          ))}
          {data.map((p, i) => {
            const y = i * rowHeight + 8;
            const values = [
              Math.max(0, p.value),
              secondary ? Math.max(0, p.secondary) : 0,
              tertiary ? Math.max(0, p.tertiary) : 0,
            ];
            let x = left;
            return (
              <g
                key={p.key}
                className={"chart-bar-group " + (onSelect ? "clickable" : "")}
                role={onSelect ? "button" : undefined}
                tabIndex={onSelect ? 0 : undefined}
                aria-label={`${p.label}: ${primary} ${format(p.value, unit)}${secondary ? ", " + secondary + " " + format(p.secondary, unit) : ""}${tertiary ? ", " + tertiary + " " + format(p.tertiary, unit) : ""}`}
                onMouseEnter={() => setHover(i)}
                onMouseLeave={() => setHover(null)}
                onFocus={() => setHover(i)}
                onBlur={() => setHover(null)}
                onClick={() => onSelect?.(p)}
                onKeyDown={(e) => {
                  if (onSelect && (e.key === "Enter" || e.key === " ")) {
                    e.preventDefault();
                    onSelect(p);
                  }
                }}
              >
                <rect
                  x="0"
                  y={y - 5}
                  width={W}
                  height="36"
                  rx="5"
                  fill={hover === i ? "#f1f5ec" : "transparent"}
                />
                <text
                  x={left - 14}
                  y={y + 17}
                  textAnchor="end"
                  className="chart-category"
                >
                  {p.label.length > 24 ? p.label.slice(0, 22) + "…" : p.label}
                </text>
                {values.map((v, j) => {
                  const start = x;
                  const width = (v / max) * plot;
                  x += width;
                  return (
                    <rect
                      key={j}
                      x={start}
                      y={y + 3}
                      width={width}
                      height="21"
                      rx="3"
                      fill={[palette[0], palette[1], "#e1e8d9"][j]}
                      opacity={hover !== null && hover !== i ? 0.55 : 1}
                      className="chart-bar-enter"
                      style={{
                        animationDelay: `${90 + i * 80 + j * 45}ms`,
                      }}
                    />
                  );
                })}
                <title>
                  {p.label}: {format(p.value, unit)}
                  {secondary
                    ? " + " + format(p.secondary, unit) + " " + secondary
                    : ""}
                  {tertiary
                    ? " + " + format(p.tertiary, unit) + " " + tertiary
                    : ""}
                </title>
              </g>
            );
          })}
        </svg>
      </div>
      <div className="chart-readout" aria-live="polite">
        {hover !== null ? (
          <>
            <strong>{data[hover].label}</strong>
            <span>
              {primary}: {format(data[hover].value, unit)}
              {secondary
                ? " · " + secondary + ": " + format(data[hover].secondary, unit)
                : ""}
              {tertiary
                ? " · " + tertiary + ": " + format(data[hover].tertiary, unit)
                : ""}
            </span>
          </>
        ) : (
          <span>
            {onSelect
              ? "Select a bar to open the underlying records."
              : "Hover over a bar to see exact values."}
            {points.length > limit
              ? " Showing the top " + limit + " of " + points.length + "."
              : ""}
          </span>
        )}
      </div>
      <DataValues
        points={points}
        unit={unit}
        secondary={secondary}
        tertiary={tertiary}
      />
    </>
  );
}
export function TrendChart({
  spending,
  income = [],
  onOpen,
}: {
  spending: Month[];
  income?: Month[];
  onOpen?: (month: string) => void;
}) {
  const years = [
    ...new Set([
      new Date().getFullYear(),
      ...spending.concat(income).map((p) => Number(p.monthKey.slice(0, 4))),
    ]),
  ].sort((a, b) => b - a);
  const [year, setYear] = useState(years[0]);
  const [active, setActive] = useState<number | null>(null);
  const canvas = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(900);
  const gradient = useId().replaceAll(":", "");
  const labels = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];
  const points = labels.map((label, i) => {
    const key = year + "-" + String(i + 1).padStart(2, "0");
    return {
      key,
      label,
      spent: spending.find((p) => p.monthKey === key)?.value ?? 0,
      income: income.find((p) => p.monthKey === key)?.value ?? 0,
    };
  });
  const maximum = Math.max(1, ...points.flatMap((p) => [p.spent, p.income]));
  const max =
    maximum > 1
      ? Math.ceil(maximum / Math.pow(10, Math.floor(Math.log10(maximum)))) *
        Math.pow(10, Math.floor(Math.log10(maximum)))
      : 1;
  const W = width,
    H = 250,
    left = 55,
    right = 20,
    top = 20,
    bottom = 35,
    plotH = H - top - bottom,
    plotW = W - left - right;
  const x = (i: number) => left + (i / 11) * plotW;
  const y = (n: number) => top + (1 - n / max) * plotH;
  const today = new Date();
  const lastRecorded = points.reduce(
    (last, p, i) => (p.spent || p.income ? i : last),
    -1,
  );
  const plotEnd = Math.max(
    lastRecorded,
    year < today.getFullYear()
      ? 11
      : year === today.getFullYear()
        ? today.getMonth()
        : 0,
  );
  const path = (key: "spent" | "income") =>
    points
      .slice(0, plotEnd + 1)
      .map((p, i) => (i ? "L" : "M") + x(i) + "," + y(p[key]))
      .join(" ");
  const any = points.some((p) => p.spent || p.income);
  useEffect(() => {
    if (!canvas.current) return;
    const observer = new ResizeObserver((entries) => {
      const next = Math.max(320, entries[0].contentRect.width);
      setWidth((previous) => (Math.abs(previous - next) > 1 ? next : previous));
    });
    observer.observe(canvas.current);
    return () => observer.disconnect();
  }, [any, year]);
  return (
    <>
      <div className="trend-toolbar">
        <div className="chart-series-key">
          <span>
            <i style={{ background: palette[0] }} />
            Spending
          </span>
          {income.length > 0 && (
            <span>
              <i style={{ background: palette[1] }} />
              Cash income
            </span>
          )}
        </div>
        <label className="chart-year">
          Year
          <select
            aria-label="Chart year"
            value={year}
            onChange={(e) => {
              setYear(Number(e.target.value));
              setActive(null);
            }}
          >
            {years.map((yr) => (
              <option key={yr}>{yr}</option>
            ))}
          </select>
        </label>
      </div>
      {any ? (
        <>
          <div className="trend-canvas" ref={canvas}>
            <svg
              viewBox={`0 0 ${W} ${H}`}
              role="img"
              aria-label={`Monthly actual spending${income.length ? " and cash income" : ""} in ${year}`}
            >
              <defs>
                <linearGradient id={gradient} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#4e8c6d" stopOpacity=".19" />
                  <stop offset="100%" stopColor="#4e8c6d" stopOpacity=".01" />
                </linearGradient>
              </defs>
              {[0, 0.25, 0.5, 0.75, 1].map((t) => (
                <g key={t}>
                  <line
                    x1={left}
                    x2={W - right}
                    y1={y(max * t)}
                    y2={y(max * t)}
                    stroke="#e9eee5"
                    strokeDasharray="3 6"
                  />
                  <text
                    x={left - 12}
                    y={y(max * t) + 4}
                    textAnchor="end"
                    className="chart-tick"
                  >
                    {compact(max * t)}
                  </text>
                </g>
              ))}
              <path
                d={path("spent") + ` L${x(plotEnd)},${y(0)} L${x(0)},${y(0)} Z`}
                fill={`url(#${gradient})`}
                className="trend-area-enter"
              />
              {income.length > 0 && (
                <path
                  d={path("income")}
                  fill="none"
                  stroke={palette[1]}
                  strokeWidth="2.5"
                  strokeDasharray="6 5"
                  strokeLinejoin="round"
                  className="trend-income-enter"
                />
              )}
              <path
                d={path("spent")}
                fill="none"
                stroke={palette[0]}
                strokeWidth="3"
                strokeLinejoin="round"
                pathLength={1}
                strokeDasharray="1"
                className="trend-line-enter"
              />
              {points.map((p, i) => (
                <g
                  key={p.key}
                  role="button"
                  tabIndex={0}
                  className="trend-month"
                  aria-label={`${labels[i]} ${year}: spending ${kes(p.spent)}, cash income ${kes(p.income)}${onOpen ? ". Open records" : ""}`}
                  onMouseEnter={() => setActive(i)}
                  onMouseLeave={() => setActive(null)}
                  onFocus={() => setActive(i)}
                  onBlur={() => setActive(null)}
                  onClick={() => {
                    setActive(i);
                    onOpen?.(p.key);
                  }}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      setActive(i);
                      onOpen?.(p.key);
                    }
                  }}
                >
                  {active === i && (
                    <line
                      x1={x(i)}
                      x2={x(i)}
                      y1={top}
                      y2={y(0)}
                      stroke="#b7c8ac"
                      className="trend-focus-line"
                    />
                  )}
                  {year === today.getFullYear() && i === lastRecorded && i <= plotEnd && (
                    <circle
                      cx={x(i)}
                      cy={y(p.spent)}
                      r="8"
                      fill="none"
                      stroke={palette[0]}
                      strokeWidth="1.5"
                      className="trend-live-pulse"
                    />
                  )}
                  {i <= plotEnd && (
                    <circle
                      cx={x(i)}
                      cy={y(p.spent)}
                      r={active === i ? 6 : p.spent ? 4 : 2}
                      fill={palette[0]}
                      stroke="#fff"
                      strokeWidth="2"
                      className="trend-point-enter"
                      style={{ animationDelay: `${500 + i * 55}ms` }}
                    />
                  )}
                  {income.length > 0 && p.income > 0 && (
                    <circle
                      cx={x(i)}
                      cy={y(p.income)}
                      r="4"
                      fill={palette[1]}
                      stroke="#fff"
                      strokeWidth="2"
                      className="trend-point-enter trend-point-income"
                      style={{ animationDelay: `${610 + i * 55}ms` }}
                    />
                  )}
                  <text
                    x={x(i)}
                    y={H - 9}
                    textAnchor="middle"
                    className="chart-tick"
                  >
                    {p.label}
                  </text>
                  <rect
                    x={x(i) - plotW / 25}
                    y={top}
                    width={plotW / 12}
                    height={plotH + 30}
                    fill="transparent"
                  />
                  <title>
                    {p.label}: {kes(p.spent)} spent · {kes(p.income)} received
                  </title>
                </g>
              ))}
            </svg>
          </div>
          <div className="trend-tooltip" aria-live="polite">
            {active !== null ? (
              <>
                <strong>
                  {labels[active]} {year}
                </strong>
                <span>
                  Spending <b>{kes(points[active].spent)}</b>
                </span>
                {income.length > 0 && (
                  <span>
                    Cash income <b>{kes(points[active].income)}</b>
                  </span>
                )}
              </>
            ) : (
              <span>
                Hover over a month for exact amounts
                {onOpen ? " · Select to view transactions" : ""}.
              </span>
            )}
          </div>
        </>
      ) : (
        <Empty label={"No financial activity recorded in " + year} />
      )}
    </>
  );
}
