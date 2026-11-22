import { useEffect, useState } from "react";
import {
  BookOpen,
  LayoutDashboard,
  Building2,
  Users,
  Wallet,
  ClipboardList,
  CheckCheck,
  Receipt,
  Truck,
  CreditCard,
  Coins,
  HeartHandshake,
  Package,
  Monitor,
  Files,
  ChartNoAxesCombined,
  History,
  Settings,
  LogOut,
  Menu,
} from "lucide-react";
import { api, Row } from "./api";
import { schemas } from "./schema";
import { Dashboard } from "./pages/Dashboard";
import { Records } from "./pages/Records";
import { Reports } from "./pages/Reports";
import { Modal } from "./components/Modal";
const nav = [
  ["dashboard", "Dashboard", LayoutDashboard],
  ["departments", "Departments", Building2],
  ["users", "Users", Users],
  ["budgets", "Budgets", Wallet],
  ["requests", "Purchase Requests", ClipboardList],
  ["approvals", "Approvals", CheckCheck],
  ["expenses", "Expenses", Receipt],
  ["suppliers", "Suppliers", Truck],
  ["payments", "Payments", CreditCard],
  ["petty-accounts", "Petty Cash", Coins],
  ["income", "Income & Donations", HeartHandshake],
  ["inventory", "Inventory", Package],
  ["assets", "Assets", Monitor],
  ["documents", "Documents", Files],
  ["reports", "Reports", ChartNoAxesCombined],
  ["audit", "Audit History", History],
  ["settings", "Settings", Settings],
] as const;
export default function App() {
  const [user, setUser] = useState<Row | null>(null);
  const [ready, setReady] = useState(false);
  const [path, setPath] = useState(location.hash.slice(1) || "dashboard");
  const [lookups, setLookups] = useState<Record<string, Row[]>>({});
  const [toast, setToast] = useState("");
  const [mobile, setMobile] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [logout, setLogout] = useState(false);
  const admin = user?.role === "ADMIN";
  useEffect(() => {
    api("auth/me")
      .then(setUser)
      .catch(() => {})
      .finally(() => setReady(true));
    const fn = () => {
      setPath(location.hash.slice(1) || "dashboard");
      setMobile(false);
    };
    const expired = () => {
      setUser(null);
      setLookups({});
    };
    window.addEventListener("session-expired", expired);
    window.addEventListener("hashchange", fn);
    return () => {
      window.removeEventListener("hashchange", fn);
      window.removeEventListener("session-expired", expired);
    };
  }, []);
  async function refresh() {
    const resources = [
      "departments",
      "categories",
      "budgets",
      "requests",
      "petty-accounts",
      "petty-transactions",
      "inventory",
      "assets",
      "locations",
      ...(user?.role === "ADMIN" ? ["suppliers", "invoices"] : []),
    ];
    const entries = await Promise.all(
      resources.map(async (r) => [r, await api("records/" + r)] as const),
    );
    setLookups(Object.fromEntries(entries));
  }
  useEffect(() => {
    if (user) refresh().catch((e) => setError(e.message));
  }, [user]);
  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(""), 5000);
      return () => clearTimeout(timer);
    }
  }, [toast]);
  function go(p: string) {
    location.hash = p;
  }
  if (!ready) return <div className="loading">Opening school accounts…</div>;
  if (!user)
    return (
      <main className="login">
        <div className="login-story">
          <div className="brand">
            <BookOpen />
            <div>
              MISSION SCHOOL<small>STEWARDSHIP & ACCOUNTABILITY</small>
            </div>
          </div>
          <div>
            <span className="eyebrow">FAITHFUL WITH EVERY RESOURCE</span>
            <h1>
              Good stewardship.
              <br />
              Stronger school.
            </h1>
            <p>
              Plan with care. Spend with purpose.
              <br />
              Keep every shilling accountable.
            </p>
          </div>
          <small>A local-first financial home for your school community.</small>
        </div>
        <div className="login-form">
          <span className="eyebrow">WELCOME BACK</span>
          <h2>Sign in to your school</h2>
          <p>Use the account provided by your administrator.</p>
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              setBusy(true);
              setError("");
              try {
                setUser(await api("auth/login", "POST", { email, password }));
                go("dashboard");
              } catch (e) {
                setError((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            <label>
              Email
              <input
                required
                type="email"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@missionschool.local"
              />
            </label>
            <label>
              Password
              <input
                required
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </label>
            {error && <p className="error">{error}</p>}
            <button className="primary" disabled={busy}>
              {busy ? "Signing in…" : "Sign in →"}
            </button>
          </form>
          <p className="muted">
            Need access? Contact the school administrator.
          </p>
        </div>
      </main>
    );
  const allowed = admin
    ? nav
    : nav.filter(([p]) =>
        [
          "dashboard",
          "departments",
          "budgets",
          "requests",
          "expenses",
          "petty-accounts",
          "inventory",
          "assets",
          "documents",
          "audit",
          "settings",
        ].includes(p),
      );
  const allowedPaths = admin
    ? Object.keys(schemas).concat("dashboard", "reports", "settings")
    : [
        ...allowed.map((n) => String(n[0])),
        "petty-transactions",
        "petty-requests",
        "movements",
        "reports",
        "income",
        "payments",
        "invoices",
      ];
  const route = path.split("?")[0];
  const current = allowedPaths.includes(route) ? route : "dashboard";
  return (
    <div className="app">
      <aside className={mobile ? "open" : ""}>
        <div className="brand">
          <BookOpen />
          <div>
            MISSION SCHOOL<small>STEWARDSHIP</small>
          </div>
        </div>
        <div className="portal-label">
          {admin ? "MANAGEMENT PORTAL" : "DEPARTMENT PORTAL"}
        </div>
        <nav>
          {allowed.map(([p, t, Icon]) => (
            <a
              key={p}
              href={"#" + p}
              className={current === p ? "selected" : ""}
            >
              <Icon size={18} />
              {!admin && p === "departments"
                ? "My Department"
                : !admin && p === "audit"
                  ? "Activity"
                  : t}
            </a>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <span className="avatar">{user.name[0]}</span>
          <div>
            <strong>{user.name}</strong>
            <small>
              {admin
                ? "School Administrator"
                : lookups.departments?.find((d) => d.id === user.departmentId)
                    ?.name}
            </small>
          </div>
          <button aria-label="Sign out" onClick={() => setLogout(true)}>
            <LogOut size={17} />
          </button>
        </div>
      </aside>
      <div className="main">
        <header>
          <div>
            <button
              className="mobile-menu"
              aria-label="Toggle navigation"
              onClick={() => setMobile(!mobile)}
            >
              <Menu />
            </button>
            <span>Mission School</span>
            <span className="header-divider">/</span>
            <strong>
              {current === "dashboard"
                ? "Overview"
                : schemas[current]?.title || current}
            </strong>
          </div>
          <div className="header-right">
            <span className="local-dot" /> Local school server{" "}
            <span className="header-divider">|</span>{" "}
            {new Date().toLocaleDateString("en-GB", {
              day: "numeric",
              month: "short",
              year: "numeric",
            })}
          </div>
        </header>
        <main className="content">
          {current === "dashboard" ? (
            <Dashboard go={go} lookups={lookups} />
          ) : current === "reports" ? (
            <Reports lookups={lookups} />
          ) : current === "settings" ? (
            <SettingsPage admin={!!admin} notify={setToast} />
          ) : (
            <Records
              key={path}
              resource={current === "approvals" ? "requests" : current}
              approvalQueue={current === "approvals"}
              lookups={lookups}
              user={user}
              refresh={refresh}
              notify={setToast}
            />
          )}
        </main>
        <footer>
          Mission School · Financial stewardship{" "}
          <span>Currency: KES · Africa/Nairobi</span>
        </footer>
      </div>
      {toast && (
        <div className="toast" role="status">
          ✓ {toast}
        </div>
      )}
      {logout && (
        <Modal title="Sign out?" onClose={() => setLogout(false)}>
          <p>Your saved records will remain available to authorised staff.</p>
          <div className="form-actions">
            <button onClick={() => setLogout(false)}>Stay signed in</button>
            <button
              className="primary"
              onClick={async () => {
                await api("auth/logout", "POST");
                setUser(null);
                setLogout(false);
              }}
            >
              Sign out
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
function SettingsPage({
  admin,
  notify,
}: {
  admin: boolean;
  notify: (s: string) => void;
}) {
  const [old, setOld] = useState("");
  const [pw, setPw] = useState("");
  const [error, setError] = useState("");
  return (
    <>
      <div className="page-heading">
        <div>
          <h1>Settings</h1>
          <p>Account security and school reference data.</p>
        </div>
      </div>
      {admin && (
        <div className="actions settings-links">
          <a className="button" href="#categories">
            Budget categories
          </a>
          <a className="button" href="#locations">
            Stock locations
          </a>
          <a className="button" href="#adjustments">
            Budget adjustment history
          </a>
        </div>
      )}
      <section className="panel narrow">
        <h2>Change password</h2>
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            try {
              await api("auth/password", "POST", {
                currentPassword: old,
                password: pw,
              });
              notify("Password changed. Please sign in again.");
              location.reload();
            } catch (e) {
              setError((e as Error).message);
            }
          }}
        >
          <label>
            Current password
            <input
              required
              type="password"
              value={old}
              onChange={(e) => setOld(e.target.value)}
            />
          </label>
          <label>
            New password
            <input
              required
              minLength={10}
              type="password"
              value={pw}
              onChange={(e) => setPw(e.target.value)}
            />
          </label>
          <p className="muted">
            At least 10 characters, uppercase, digit and symbol.
          </p>
          {error && <p className="error">{error}</p>}
          <button className="primary">Update password</button>
        </form>
      </section>
    </>
  );
}
