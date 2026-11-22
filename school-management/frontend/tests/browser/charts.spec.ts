import { test, expect } from "@playwright/test";
async function login(
  page: any,
  email = "admin@missionschool.local",
  password = "Admin123!",
) {
  await page.goto("/");
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
}
test("Dashboard charts render actual values and drill into records", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await login(page);
  const data = await (await page.request.get("/api/insights/dashboard")).json();
  await expect(page.locator('svg[role="img"]')).toHaveCount(5);
  expect(
    data.charts.spending.trend.reduce((s: number, r: any) => s + r.value, 0),
  ).toBe(data.spent);
  expect(
    data.charts.budgets.comparison.reduce(
      (s: number, r: any) => s + r.value,
      0,
    ),
  ).toBe(data.spent);
  await page.getByRole("button", { name: "Full budget", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Full budget", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await page.locator(".trend-month").nth(10).focus();
  await expect(page.locator(".trend-tooltip")).toContainText("Nov");
  await page.screenshot({
    path: "../tests/charts-dashboard.png",
    fullPage: true,
  });
  await page.locator(".trend-month").nth(10).click();
  await expect(
    page.getByRole("heading", { name: "Reports", exact: true }),
  ).toBeVisible();
  await expect(page.getByLabel("From date")).toHaveValue("2026-11-01");
  await expect(page.getByLabel("To date")).toHaveValue("2026-11-30");
  await expect(page.getByText("Preparing report…")).toHaveCount(0);
  expect(errors).toEqual([]);
});
test("Report graphs match filtered records; modules have visual summaries", async ({
  page,
}) => {
  await login(page);
  await page.goto("/#reports");
  await page
    .getByRole("combobox", { name: "Report", exact: true })
    .selectOption("Department Expenditure");
  await expect(page.getByText("Preparing report…")).toHaveCount(0);
  const department = (
    await (await page.request.get("/api/records/departments")).json()
  ).find((d: any) => d.name === "Maintenance");
  await page
    .getByRole("combobox", { name: "Department", exact: true })
    .selectOption(String(department.id));
  await expect(page.getByText("Preparing report…")).toHaveCount(0);
  const query = "name=Department%20Expenditure&departmentId=" + department.id;
  const rows = await (
    await page.request.get("/api/insights/reports?" + query)
  ).json();
  const chart = await (
    await page.request.get("/api/insights/report-charts?" + query)
  ).json();
  expect(chart.records).toBe(rows.length);
  expect(chart.comparison.reduce((s: number, p: any) => s + p.value, 0)).toBe(
    rows.reduce((s: number, r: any) => s + r.amount, 0),
  );
  await page.screenshot({
    path: "../tests/charts-reports.png",
    fullPage: true,
  });
  for (const resource of [
    "budgets",
    "requests",
    "expenses",
    "payments",
    "income",
    "inventory",
    "assets",
    "petty-accounts",
  ]) {
    await page.goto("/#" + resource);
    await expect(page.getByText("Preparing visual overview…")).toHaveCount(0);
    await expect(page.locator(".module-charts")).toBeVisible();
    await expect(page.locator('[role="alert"]')).toHaveCount(0);
  }
});
test("Department chart endpoints stay scoped to the signed-in department", async ({
  page,
}) => {
  await login(page, "kitchen@missionschool.local", "SchoolDemo123!");
  const me = await (await page.request.get("/api/auth/me")).json();
  for (const resource of ["budgets", "requests", "expenses", "payments"]) {
    const response = await page.request.get("/api/insights/charts/" + resource);
    expect(response.ok()).toBeTruthy();
    const chart = await response.json();
    expect(
      chart.comparison.every((p: any) => p.key === String(me.departmentId)),
    ).toBeTruthy();
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBeTruthy();
});
